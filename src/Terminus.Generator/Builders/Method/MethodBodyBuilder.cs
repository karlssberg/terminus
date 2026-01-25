using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Strategies;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Method;

/// <summary>
/// Builds method body statements for facade method implementations.
/// </summary>
internal sealed class MethodBodyBuilder(IServiceResolutionStrategy serviceResolution)
{
    private readonly InvocationBuilder _invocationBuilder = new(serviceResolution);

    /// <summary>
    /// Builds the complete method body statements.
    /// </summary>
    public IEnumerable<StatementSyntax> BuildMethodBody(
        FacadeInterfaceInfo facadeInfo,
        AggregatedMethodGroup methodGroup)
    {
        var hasInterceptors = facadeInfo.Features.HasInterceptors;

        // For aggregated methods with interceptors, wrap in interceptor pipeline with per-handler filtering
        if (methodGroup.RequiresAggregation && hasInterceptors)
        {
            foreach (var statement in BuildInterceptorWrappedAggregatedMethodBody(facadeInfo, methodGroup))
            {
                yield return statement;
            }
            yield break;
        }

        // For aggregated methods (without interceptors), generate yield return statements
        if (methodGroup.RequiresAggregation)
        {
            foreach (var statement in BuildAggregatedMethodBody(facadeInfo, methodGroup))
            {
                yield return statement;
            }
            yield break;
        }

        // For single methods with interceptors, wrap in interceptor pipeline
        if (hasInterceptors)
        {
            foreach (var statement in BuildInterceptorWrappedMethodBody(facadeInfo, methodGroup.PrimaryMethod))
            {
                yield return statement;
            }
            yield break;
        }

        // For single methods (without metadata and without interceptors), use existing logic
        var methodInfo = methodGroup.PrimaryMethod;

        // Handle CancellationToken.ThrowIfCancellationRequested() first for static scoped methods
        var cancellationTokens = methodInfo.MethodSymbol.Parameters
            .Where(p => !p.IsParams && p.Type.ToDisplayString() == typeof(CancellationToken).FullName)
            .ToList();

        if (cancellationTokens.Count == 1 && methodInfo.MethodSymbol.IsStatic)
        {
            var parameterName = cancellationTokens[0].Name;
            yield return ParseStatement($"{parameterName}.ThrowIfCancellationRequested();");
        }

        // Build the invocation expression
        var invocationExpression = _invocationBuilder.BuildInvocation(facadeInfo, methodInfo);

        // Return or expression statement depending on void / async kind
        yield return methodInfo.ReturnTypeKind switch
        {
            ReturnTypeKind.Void =>
                ExpressionStatement(invocationExpression),
            ReturnTypeKind.Task =>
                ExpressionStatement(AwaitExpression(invocationExpression)),
            ReturnTypeKind.ValueTask =>
                ExpressionStatement(AwaitExpression(invocationExpression)),
            ReturnTypeKind.TaskWithResult =>
                ReturnStatement(AwaitExpression(invocationExpression)),
            ReturnTypeKind.ValueTaskWithResult =>
                ReturnStatement(AwaitExpression(invocationExpression)),
            ReturnTypeKind.AsyncEnumerable when facadeInfo.Features.IsScoped =>
                BuildAsyncEnumerableProxyStatement(facadeInfo, methodInfo),
            _ => ReturnStatement(invocationExpression)
        };
    }

    /// <summary>
    /// Builds method body for methods with interceptors.
    /// </summary>
    private IEnumerable<StatementSyntax> BuildInterceptorWrappedMethodBody(
        FacadeInterfaceInfo facadeInfo,
        CandidateMethodInfo methodInfo)
    {
        var interfaceName = facadeInfo.InterfaceSymbol
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var containingTypeName = methodInfo.MethodSymbol.ContainingType
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var attributeTypeName = methodInfo.AttributeData.AttributeClass?
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "global::System.Attribute";

        var methodName = methodInfo.MethodSymbol.Name;
        var parameters = methodInfo.MethodSymbol.Parameters;
        var argumentsArray = parameters.Length > 0
            ? string.Join(", ", parameters.Select(p => p.Name.EscapeIdentifier()))
            : "";

        var returnTypeKindName = methodInfo.ReturnTypeKind switch
        {
            ReturnTypeKind.Void => "Void",
            ReturnTypeKind.Result => "Result",
            ReturnTypeKind.Task => "Task",
            ReturnTypeKind.ValueTask => "Task",
            ReturnTypeKind.TaskWithResult => "TaskWithResult",
            ReturnTypeKind.ValueTaskWithResult => "TaskWithResult",
            ReturnTypeKind.AsyncEnumerable => "AsyncEnumerable",
            _ => "Void"
        };

        var isStatic = methodInfo.MethodSymbol.IsStatic;
        var attributeInstantiation = BuildAttributeInstantiation(methodInfo);

        // Build handler descriptor array (single element for non-aggregated methods)
        var handlerDescriptorStatement = ParseStatement(
            $$"""
            var handlers = new global::Terminus.FacadeHandlerDescriptor[]
            {
                new global::Terminus.FacadeHandlerDescriptor(
                    typeof({{containingTypeName}}),
                    {{attributeInstantiation.ToFullString()}},
                    isStatic: {{isStatic.ToString().ToLowerInvariant()}})
            };
            """);
        yield return handlerDescriptorStatement;

        // Build the context creation statement
        var contextStatement = ParseStatement(
            $$"""
            var context = new global::Terminus.FacadeInvocationContext(
                _serviceProvider,
                typeof({{interfaceName}}).GetMethod("{{methodName}}")!,
                new object?[] { {{argumentsArray}} },
                typeof({{containingTypeName}}),
                {{attributeInstantiation.ToFullString()}},
                new global::System.Collections.Generic.Dictionary<string, object?>(),
                global::Terminus.ReturnTypeKind.{{returnTypeKindName}},
                handlers,
                isAggregated: false);
            """);
        yield return contextStatement;

        // Build the invocation and wrapping based on return type
        var invocation = _invocationBuilder.BuildInvocation(facadeInfo, methodInfo, includeConfigureAwait: false);

        switch (methodInfo.ReturnTypeKind)
        {
            case ReturnTypeKind.Void:
            {
                yield return ParseStatement(
                    $$"""
                    ExecuteWithInterceptors<object>(
                        context,
                        () =>
                        {
                            {{invocation.ToFullString()}};
                            return default;
                        });
                    """);
                break;
            }
            case ReturnTypeKind.Result:
            {
                var returnType = methodInfo.MethodSymbol.ReturnType
                    .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                yield return ParseStatement(
                    $$"""
                    return ExecuteWithInterceptors<{{returnType}}>(
                        context,
                        () => {{invocation.ToFullString()}})!;
                    """);
                break;
            }
            case ReturnTypeKind.Task:
            {
                yield return ParseStatement(
                    $$"""
                    await ExecuteWithInterceptorsAsync<object>(
                        context,
                        async () =>
                        {
                            await {{invocation.ToFullString()}}.ConfigureAwait(false);
                            return default;
                        }).ConfigureAwait(false);
                    """);
                break;
            }
            case ReturnTypeKind.ValueTask:
            {
                yield return ParseStatement(
                    $$"""
                    await ExecuteWithInterceptorsAsync<object>(
                        context,
                        async () =>
                        {
                            await {{invocation.ToFullString()}}.ConfigureAwait(false);
                            return default;
                        }).ConfigureAwait(false);
                    """);
                break;
            }
            case ReturnTypeKind.TaskWithResult:
            case ReturnTypeKind.ValueTaskWithResult:
            {
                var returnType = ((INamedTypeSymbol)methodInfo.MethodSymbol.ReturnType).TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                yield return ParseStatement(
                    $$"""
                    return (await ExecuteWithInterceptorsAsync<{{returnType}}>(
                        context,
                        async () => await {{invocation.ToFullString()}}.ConfigureAwait(false)).ConfigureAwait(false))!;
                    """);
                break;
            }
            case ReturnTypeKind.AsyncEnumerable:
            {
                var returnType = ((INamedTypeSymbol)methodInfo.MethodSymbol.ReturnType).TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                yield return ParseStatement(
                    $$"""
                    return ExecuteWithInterceptorsStream<{{returnType}}>(
                        context,
                        () => {{invocation.ToFullString()}});
                    """);
                break;
            }
        }
    }

    private IEnumerable<StatementSyntax> BuildAggregatedMethodBody(
        FacadeInterfaceInfo facadeInfo,
        AggregatedMethodGroup methodGroup)
    {
        var primaryMethod = methodGroup.PrimaryMethod;
        var returnTypeKind = primaryMethod.ReturnTypeKind;

        switch (returnTypeKind)
        {
            // For void methods, just execute all handlers in sequence
            case ReturnTypeKind.Void:
            {
                foreach (var statement in methodGroup.Methods.Select(method =>
                    ExpressionStatement(_invocationBuilder.BuildInvocation(facadeInfo, method))))
                {
                    yield return statement;
                }
                yield break;
            }
            // For result methods (T), yield return each result
            case ReturnTypeKind.Result:
            {
                foreach (var method in methodGroup.Methods)
                {
                    var invocation = _invocationBuilder.BuildInvocation(facadeInfo, method);
                    yield return YieldStatement(SyntaxKind.YieldReturnStatement, invocation);
                }
                yield break;
            }
            // For async result methods (Task<T>, ValueTask<T>), yield return await each result
            case ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult:
            {
                foreach (var method in methodGroup.Methods)
                {
                    var invocation = _invocationBuilder.BuildInvocation(facadeInfo, method);
                    var awaitedInvocation = AwaitExpression(invocation);
                    yield return YieldStatement(SyntaxKind.YieldReturnStatement, awaitedInvocation);
                }
                yield break;
            }
            // For Task/ValueTask without results, await all
            case ReturnTypeKind.Task or ReturnTypeKind.ValueTask:
            {
                foreach (var statement in methodGroup.Methods.Select(method =>
                    ExpressionStatement(AwaitExpression(_invocationBuilder.BuildInvocation(facadeInfo, method)))))
                {
                    yield return statement;
                }
                yield break;
            }
            // For other return types (e.g., AsyncEnumerable), execute all
            default:
            {
                foreach (var statement in methodGroup.Methods.Select(method =>
                    ExpressionStatement(_invocationBuilder.BuildInvocation(facadeInfo, method))))
                {
                    yield return statement;
                }
                yield break;
            }
        }
    }

    /// <summary>
    /// Builds method body for aggregated methods with interceptors and per-handler filtering.
    /// </summary>
    private IEnumerable<StatementSyntax> BuildInterceptorWrappedAggregatedMethodBody(
        FacadeInterfaceInfo facadeInfo,
        AggregatedMethodGroup methodGroup)
    {
        var primaryMethod = methodGroup.PrimaryMethod;
        var interfaceName = facadeInfo.InterfaceSymbol
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = primaryMethod.MethodSymbol.Name;
        var parameters = primaryMethod.MethodSymbol.Parameters;
        var argumentsArray = parameters.Length > 0
            ? string.Join(", ", parameters.Select(p => p.Name.EscapeIdentifier()))
            : "";

        var returnTypeKindName = primaryMethod.ReturnTypeKind switch
        {
            ReturnTypeKind.Void => "Void",
            ReturnTypeKind.Result => "Result",
            ReturnTypeKind.Task => "Task",
            ReturnTypeKind.ValueTask => "Task",
            ReturnTypeKind.TaskWithResult => "TaskWithResult",
            ReturnTypeKind.ValueTaskWithResult => "TaskWithResult",
            ReturnTypeKind.AsyncEnumerable => "AsyncEnumerable",
            _ => "Void"
        };

        // Build handler descriptors for all methods in the group
        var handlerDescriptors = new List<string>();
        foreach (var method in methodGroup.Methods)
        {
            var containingType = method.MethodSymbol.ContainingType
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var attributeInstantiation = BuildAttributeInstantiation(method);
            var isStatic = method.MethodSymbol.IsStatic;

            handlerDescriptors.Add(
                $$"""
                new global::Terminus.FacadeHandlerDescriptor(
                        typeof({{containingType}}),
                        {{attributeInstantiation.ToFullString()}},
                        isStatic: {{isStatic.ToString().ToLowerInvariant()}})
                """);
        }

        var handlersArrayCode = string.Join(",\n        ", handlerDescriptors);

        // Generate handler descriptors array statement
        yield return ParseStatement(
            $$"""
            var handlers = new global::Terminus.FacadeHandlerDescriptor[]
            {
                {{handlersArrayCode}}
            };
            """);

        // Generate context creation statement
        var primaryContainingType = primaryMethod.MethodSymbol.ContainingType
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var primaryAttributeInstantiation = BuildAttributeInstantiation(primaryMethod);

        yield return ParseStatement(
            $$"""
            var context = new global::Terminus.FacadeInvocationContext(
                _serviceProvider,
                typeof({{interfaceName}}).GetMethod("{{methodName}}")!,
                new object?[] { {{argumentsArray}} },
                typeof({{primaryContainingType}}),
                {{primaryAttributeInstantiation.ToFullString()}},
                new global::System.Collections.Generic.Dictionary<string, object?>(),
                global::Terminus.ReturnTypeKind.{{returnTypeKindName}},
                handlers,
                isAggregated: true);
            """);

        // Generate interceptor-wrapped aggregation based on return type
        switch (primaryMethod.ReturnTypeKind)
        {
            case ReturnTypeKind.Void:
                yield return ParseStatement(
                    $$"""
                    ExecuteWithInterceptors<object>(
                        context,
                        () =>
                        {
                            var activeHandlers = FilterHandlers(context);
                            foreach (var handler in activeHandlers)
                            {
                                {{BuildHandlerDispatchCases(facadeInfo, methodGroup, isVoid: true)}}
                            }
                            return default;
                        });
                    """);
                break;

            case ReturnTypeKind.Result:
            {
                // For result methods, we need to yield return each result
                var returnType = primaryMethod.MethodSymbol.ReturnType
                    .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                var dispatchCases = BuildHandlerDispatchCasesForResult(facadeInfo, methodGroup);

                yield return ParseStatement(
                    $$"""
                    return ExecuteWithInterceptors<global::System.Collections.Generic.IEnumerable<{{returnType}}>>(
                        context,
                        () => QueryInternal())!;
                    """);

                // Add the helper method for yielding results
                yield return ParseStatement(
                    $$"""
                    global::System.Collections.Generic.IEnumerable<{{returnType}}> QueryInternal()
                    {
                        var activeHandlers = FilterHandlers(context);
                        foreach (var handler in activeHandlers)
                        {
                            {{dispatchCases}}
                        }
                    }
                    """);
                break;
            }

            case ReturnTypeKind.Task or ReturnTypeKind.ValueTask:
                yield return ParseStatement(
                    $$"""
                    await ExecuteWithInterceptorsAsync<object>(
                        context,
                        async () =>
                        {
                            var activeHandlers = FilterHandlers(context);
                            foreach (var handler in activeHandlers)
                            {
                                {{BuildHandlerDispatchCases(facadeInfo, methodGroup, isVoid: true, isAsync: true)}}
                            }
                            return default;
                        }).ConfigureAwait(false);
                    """);
                break;

            case ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult:
            {
                // For async result methods, we need to yield return await each result
                var asyncReturnType = ((INamedTypeSymbol)primaryMethod.MethodSymbol.ReturnType).TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                var asyncDispatchCases = BuildHandlerDispatchCasesForAsyncResult(facadeInfo, methodGroup);

                yield return ParseStatement(
                    $$"""
                    return ExecuteWithInterceptorsStream<{{asyncReturnType}}>(
                        context,
                        () => QueryInternalAsync());
                    """);

                // Add the helper method for yielding async results
                yield return ParseStatement(
                    $$"""
                    async global::System.Collections.Generic.IAsyncEnumerable<{{asyncReturnType}}> QueryInternalAsync()
                    {
                        var activeHandlers = FilterHandlers(context);
                        foreach (var handler in activeHandlers)
                        {
                            {{asyncDispatchCases}}
                        }
                    }
                    """);
                break;
            }
        }
    }

    /// <summary>
    /// Builds the handler dispatch switch cases for Result methods (yielding values).
    /// </summary>
    private string BuildHandlerDispatchCasesForResult(
        FacadeInterfaceInfo facadeInfo,
        AggregatedMethodGroup methodGroup)
    {
        var cases = new List<string>();

        foreach (var method in methodGroup.Methods)
        {
            var containingType = method.MethodSymbol.ContainingType
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var invocation = _invocationBuilder.BuildInvocation(facadeInfo, method).ToFullString();

            cases.Add(
                $$"""
                if (handler.TargetType == typeof({{containingType}}))
                                yield return {{invocation}};
                """);
        }

        return string.Join("\n            else ", cases);
    }

    /// <summary>
    /// Builds the handler dispatch switch cases for async result methods (yielding values).
    /// </summary>
    private string BuildHandlerDispatchCasesForAsyncResult(
        FacadeInterfaceInfo facadeInfo,
        AggregatedMethodGroup methodGroup)
    {
        var cases = new List<string>();

        foreach (var method in methodGroup.Methods)
        {
            var containingType = method.MethodSymbol.ContainingType
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var invocation = _invocationBuilder.BuildInvocation(facadeInfo, method).ToFullString();

            cases.Add(
                $$"""
                if (handler.TargetType == typeof({{containingType}}))
                                yield return await {{invocation}}.ConfigureAwait(false);
                """);
        }

        return string.Join("\n            else ", cases);
    }

    /// <summary>
    /// Builds the handler dispatch switch cases for aggregated methods with filtering.
    /// </summary>
    private string BuildHandlerDispatchCases(
        FacadeInterfaceInfo facadeInfo,
        AggregatedMethodGroup methodGroup,
        bool isVoid = false,
        bool isAsync = false)
    {
        var cases = new List<string>();

        foreach (var method in methodGroup.Methods)
        {
            var containingType = method.MethodSymbol.ContainingType
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var invocation = _invocationBuilder.BuildInvocation(facadeInfo, method).ToFullString();

            if (isAsync && !isVoid)
            {
                // Async with result - not used in this context, handled separately
                continue;
            }
            else if (isAsync)
            {
                // Async void
                cases.Add(
                    $$"""
                    if (handler.TargetType == typeof({{containingType}}))
                                    await {{invocation}}.ConfigureAwait(false);
                    """);
            }
            else if (!isVoid)
            {
                // Sync with result - not used in this context, handled separately
                continue;
            }
            else
            {
                // Sync void
                cases.Add(
                    $$"""
                    if (handler.TargetType == typeof({{containingType}}))
                                    {{invocation}};
                    """);
            }
        }

        return string.Join("\n                else ", cases);
    }

    /// <summary>
    /// Builds an expression that instantiates the attribute with its original arguments.
    /// </summary>
    private static ExpressionSyntax BuildAttributeInstantiation(CandidateMethodInfo method)
    {
        var attributeData = method.AttributeData;
        var attributeClass = attributeData.AttributeClass;

        if (attributeClass == null)
            return LiteralExpression(SyntaxKind.NullLiteralExpression);

        var attributeTypeName = attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Build constructor arguments
        var constructorArgs = attributeData.ConstructorArguments
            .Select(FormatTypedConstant)
            .ToList();

        // Build named arguments (property initializers)
        var namedArgs = attributeData.NamedArguments
            .Select(kvp => $"{kvp.Key} = {FormatTypedConstant(kvp.Value)}")
            .ToList();

        if (constructorArgs.Count == 0 && namedArgs.Count == 0)
        {
            // Simple: new AttributeType()
            return ParseExpression($"new {attributeTypeName}()");
        }

        if (namedArgs.Count == 0)
        {
            // Constructor args only: new AttributeType(arg1, arg2)
            return ParseExpression($"new {attributeTypeName}({string.Join(", ", constructorArgs)})");
        }

        if (constructorArgs.Count == 0)
        {
            // Named args only (object initializer): new AttributeType { Prop1 = val1, Prop2 = val2 }
            return ParseExpression($"new {attributeTypeName} {{ {string.Join(", ", namedArgs)} }}");
        }

        // Both constructor and named args: new AttributeType(arg1) { Prop1 = val1 }
        return ParseExpression($"new {attributeTypeName}({string.Join(", ", constructorArgs)}) {{ {string.Join(", ", namedArgs)} }}");
    }

    private static string FormatTypedConstant(TypedConstant constant)
    {
        return constant.Kind switch
        {
            TypedConstantKind.Primitive => FormatPrimitive(constant.Value),
            TypedConstantKind.Enum => $"({constant.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){constant.Value}",
            TypedConstantKind.Type => $"typeof({((ITypeSymbol)constant.Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})",
            TypedConstantKind.Array => $"new[] {{ {string.Join(", ", constant.Values.Select(FormatTypedConstant))} }}",
            _ => constant.Value?.ToString() ?? "null"
        };
    }

    private static string FormatPrimitive(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"",
            bool b => b ? "true" : "false",
            char c => $"'{c}'",
            float f => $"{f}f",
            double d => $"{d}d",
            decimal m => $"{m}m",
            long l => $"{l}L",
            ulong ul => $"{ul}UL",
            _ => value.ToString()!
        };
    }

    private static StatementSyntax BuildAsyncEnumerableProxyStatement(
        FacadeInterfaceInfo facadeInfo,
        CandidateMethodInfo methodInfo)
    {
        // For scoped async streams, proxy enumeration via the facade interface within the scope
        var serviceResolution = ServiceResolutionStrategyFactory.GetStrategy(facadeInfo, methodInfo);
        var instanceExpression = serviceResolution.GetServiceExpression(facadeInfo, methodInfo);
        var parameters = string.Join(", ", methodInfo.MethodSymbol.Parameters.Select(p => p.Name));

        var methodName = GetMethodName(methodInfo);

        return ParseStatement(
            $$"""
            await foreach (var item in {{instanceExpression.ToFullString()}}.{{methodName}}({{parameters}}))
            {
                yield return item;
            }
            """);
    }

    private static string GetMethodName(CandidateMethodInfo methodInfo)
    {
        var methodSymbol = methodInfo.MethodSymbol;
        
        if (!methodSymbol.IsGenericMethod)
            return methodSymbol.Name;
        
        var typeArgs = string.Join(", ", methodSymbol.TypeParameters.Select(tp => tp.Name));
        return $"{methodSymbol.Name}<{typeArgs}>";
    }
}
