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
        var attributeInstantiation = BuildAttributeInstantiation(methodInfo);

        var methodName = methodInfo.MethodSymbol.Name;
        var parameters = methodInfo.MethodSymbol.Parameters;
        var argumentsArray = parameters.Length > 0
            ? string.Join(", ", parameters.Select(p => p.Name.EscapeIdentifier()))
            : "";

        var returnTypeKindName = GetReturnTypeKindName(methodInfo.ReturnTypeKind);
        var isStatic = methodInfo.MethodSymbol.IsStatic;

        // Generic facade support: determine if we should use strongly-typed context
        var isGenericFacade = facadeInfo.IsGenericFacade;
        var attributeTypeName = isGenericFacade && facadeInfo.FacadeMethodAttributeTypes.Length > 0
            ? facadeInfo.FacadeMethodAttributeTypes[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : null;

        // Build the invocation expression
        var invocation = _invocationBuilder.BuildInvocation(facadeInfo, methodInfo, includeConfigureAwait: false);
        var invocationCode = invocation.ToFullString();

        // Generate code based on return type
        switch (methodInfo.ReturnTypeKind)
        {
            case ReturnTypeKind.Void:
            {
                // Void handler descriptor with Action invoke
                yield return ParseStatement(
                    $$"""
                    var handlers = new global::Terminus.FacadeVoidHandlerDescriptor[]
                    {
                        new global::Terminus.FacadeVoidHandlerDescriptor(typeof({{containingTypeName}}), {{attributeInstantiation.ToFullString()}}, isStatic: {{isStatic.ToString().ToLowerInvariant()}}, () => {{invocationCode}})
                    };
                    """);
                yield return BuildContextCreationStatement(interfaceName, methodName, argumentsArray, containingTypeName, attributeInstantiation, returnTypeKindName, isGenericFacade, attributeTypeName);
                yield return ParseStatement(
                    """
                    ExecuteWithVoidInterceptors(context, handlers => ((global::Terminus.FacadeVoidHandlerDescriptor)(handlers ?? context.Handlers)[0]).Invoke());
                    """);
                break;
            }
            case ReturnTypeKind.Result:
            {
                var returnType = methodInfo.MethodSymbol.ReturnType
                    .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                yield return ParseStatement(
                    $$"""
                    var handlers = new global::Terminus.FacadeSyncHandlerDescriptor<{{returnType}}>[]
                    {
                        new global::Terminus.FacadeSyncHandlerDescriptor<{{returnType}}>(typeof({{containingTypeName}}), {{attributeInstantiation.ToFullString()}}, isStatic: {{isStatic.ToString().ToLowerInvariant()}}, () => {{invocationCode}})
                    };
                    """);
                yield return BuildContextCreationStatement(interfaceName, methodName, argumentsArray, containingTypeName, attributeInstantiation, returnTypeKindName, isGenericFacade, attributeTypeName);
                yield return ParseStatement(
                    $$"""
                    return ExecuteWithInterceptors<{{returnType}}>(context, handlers => ((global::Terminus.FacadeSyncHandlerDescriptor<{{returnType}}>)(handlers ?? context.Handlers)[0]).Invoke());
                    """);
                break;
            }
            case ReturnTypeKind.Task:
            case ReturnTypeKind.ValueTask:
            {
                // Async void handler descriptor with Func<Task> invoke
                yield return ParseStatement(
                    $$"""
                    var handlers = new global::Terminus.FacadeAsyncVoidHandlerDescriptor[]
                    {
                        new global::Terminus.FacadeAsyncVoidHandlerDescriptor(typeof({{containingTypeName}}), {{attributeInstantiation.ToFullString()}}, isStatic: {{isStatic.ToString().ToLowerInvariant()}}, async () => await {{invocationCode}}.ConfigureAwait(false))
                    };
                    """);
                yield return BuildContextCreationStatement(interfaceName, methodName, argumentsArray, containingTypeName, attributeInstantiation, returnTypeKindName, isGenericFacade, attributeTypeName);
                yield return ParseStatement(
                    """
                    await ExecuteWithAsyncVoidInterceptors(context, async handlers => await ((global::Terminus.FacadeAsyncVoidHandlerDescriptor)(handlers ?? context.Handlers)[0]).InvokeAsync().ConfigureAwait(false)).ConfigureAwait(false);
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
                    var handlers = new global::Terminus.FacadeAsyncHandlerDescriptor<{{returnType}}>[]
                    {
                        new global::Terminus.FacadeAsyncHandlerDescriptor<{{returnType}}>(typeof({{containingTypeName}}), {{attributeInstantiation.ToFullString()}}, isStatic: {{isStatic.ToString().ToLowerInvariant()}}, async () => await {{invocationCode}}.ConfigureAwait(false))
                    };
                    """);
                yield return BuildContextCreationStatement(interfaceName, methodName, argumentsArray, containingTypeName, attributeInstantiation, returnTypeKindName, isGenericFacade, attributeTypeName);
                yield return ParseStatement(
                    $$"""
                    return await ExecuteWithInterceptorsAsync<{{returnType}}>(context, async handlers => await ((global::Terminus.FacadeAsyncHandlerDescriptor<{{returnType}}>)(handlers ?? context.Handlers)[0]).InvokeAsync()).ConfigureAwait(false);
                    """);
                break;
            }
            case ReturnTypeKind.AsyncEnumerable:
            {
                var itemType = ((INamedTypeSymbol)methodInfo.MethodSymbol.ReturnType).TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                yield return ParseStatement(
                    $$"""
                    var handlers = new global::Terminus.FacadeStreamHandlerDescriptor<{{itemType}}>[]
                    {
                        new global::Terminus.FacadeStreamHandlerDescriptor<{{itemType}}>(typeof({{containingTypeName}}), {{attributeInstantiation.ToFullString()}}, isStatic: {{isStatic.ToString().ToLowerInvariant()}}, () => {{invocationCode}})
                    };
                    """);
                yield return BuildContextCreationStatement(interfaceName, methodName, argumentsArray, containingTypeName, attributeInstantiation, returnTypeKindName, isGenericFacade, attributeTypeName);
                yield return ParseStatement(
                    $$"""
                    return ExecuteWithInterceptorsStream<{{itemType}}>(context, handlers => ((global::Terminus.FacadeStreamHandlerDescriptor<{{itemType}}>)(handlers ?? context.Handlers)[0]).Invoke());
                    """);
                break;
            }
        }
    }

    private static StatementSyntax BuildContextCreationStatement(
        string interfaceName,
        string methodName,
        string argumentsArray,
        string containingTypeName,
        ExpressionSyntax attributeInstantiation,
        string returnTypeKindName,
        bool isGenericFacade,
        string? attributeTypeName = null)
    {
        var contextTypeName = isGenericFacade && attributeTypeName != null
            ? $"global::Terminus.FacadeInvocationContext<{attributeTypeName}>"
            : "global::Terminus.FacadeInvocationContext";

        return ParseStatement(
            $$"""
            var context = new {{contextTypeName}}(
                _serviceProvider,
                typeof({{interfaceName}}).GetMethod("{{methodName}}")!,
                new object? [] { {{argumentsArray}} },
                typeof({{containingTypeName}}),
                {{attributeInstantiation.ToFullString()}},
                new global::System.Collections.Generic.Dictionary<string, object?>(),
                global::Terminus.ReturnTypeKind.{{returnTypeKindName}},
                handlers,
                isAggregated: false);
            """);
    }

    private static string GetReturnTypeKindName(ReturnTypeKind returnTypeKind) =>
        returnTypeKind switch
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

    private IEnumerable<StatementSyntax> BuildAggregatedMethodBody(
        FacadeInterfaceInfo facadeInfo,
        AggregatedMethodGroup methodGroup)
    {
        var primaryMethod = methodGroup.PrimaryMethod;
        var returnTypeKind = primaryMethod.ReturnTypeKind;

        // AggregationReturnTypeStrategy enum values: Collection = 0, First = 1
        const int FirstStrategy = 1;
        var isFirstStrategy = facadeInfo.Features.AggregationReturnTypeStrategy == FirstStrategy;

        // For "First" strategy, execute only the first handler
        if (isFirstStrategy)
        {
            var firstMethod = methodGroup.Methods[0];

            switch (returnTypeKind)
            {
                case ReturnTypeKind.Void:
                    yield return ExpressionStatement(_invocationBuilder.BuildInvocation(facadeInfo, firstMethod));
                    yield break;
                case ReturnTypeKind.Result:
                    yield return ReturnStatement(_invocationBuilder.BuildInvocation(facadeInfo, firstMethod));
                    yield break;
                case ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult:
                    yield return ReturnStatement(AwaitExpression(_invocationBuilder.BuildInvocation(facadeInfo, firstMethod)));
                    yield break;
                case ReturnTypeKind.Task or ReturnTypeKind.ValueTask:
                    yield return ExpressionStatement(AwaitExpression(_invocationBuilder.BuildInvocation(facadeInfo, firstMethod)));
                    yield break;
                default:
                    yield return ExpressionStatement(_invocationBuilder.BuildInvocation(facadeInfo, firstMethod));
                    yield break;
            }
        }

        // Existing "Collection" strategy logic below (unchanged)
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

        var returnTypeKindName = GetReturnTypeKindName(primaryMethod.ReturnTypeKind);

        // Generic facade support: determine if we should use strongly-typed context
        var isGenericFacade = facadeInfo.IsGenericFacade;
        var attributeTypeName = isGenericFacade && facadeInfo.FacadeMethodAttributeTypes.Length > 0
            ? facadeInfo.FacadeMethodAttributeTypes[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : null;
        var contextTypeName = isGenericFacade && attributeTypeName != null
            ? $"global::Terminus.FacadeInvocationContext<{attributeTypeName}>"
            : "global::Terminus.FacadeInvocationContext";

        // Build typed handler descriptors with invoke delegates for all methods in the group
        var handlerDescriptors = new List<string>();
        foreach (var method in methodGroup.Methods)
        {
            var containingType = method.MethodSymbol.ContainingType
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var attributeInstantiation = BuildAttributeInstantiation(method);
            var isStatic = method.MethodSymbol.IsStatic;
            var invocation = _invocationBuilder.BuildInvocation(facadeInfo, method, includeConfigureAwait: false).ToFullString();

            var descriptorType = GetHandlerDescriptorType(primaryMethod.ReturnTypeKind, primaryMethod.MethodSymbol.ReturnType);
            var invokeCode = GetHandlerInvokeCode(primaryMethod.ReturnTypeKind, invocation);

            handlerDescriptors.Add(
                $$"""
                new {{descriptorType}}(typeof({{containingType}}), {{attributeInstantiation.ToFullString()}}, isStatic: {{isStatic.ToString().ToLowerInvariant()}}, {{invokeCode}})
                """);
        }

        var handlersArrayCode = string.Join(",\n        ", handlerDescriptors);
        var handlerArrayType = GetHandlerDescriptorType(primaryMethod.ReturnTypeKind, primaryMethod.MethodSymbol.ReturnType);

        // Generate handler descriptors array statement
        yield return ParseStatement(
            $$"""
            var handlers = new {{handlerArrayType}}[]
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
            var context = new {{contextTypeName}}(
                _serviceProvider,
                typeof({{interfaceName}}).GetMethod("{{methodName}}")!,
                new object? [] { {{argumentsArray}} },
                typeof({{primaryContainingType}}),
                {{primaryAttributeInstantiation.ToFullString()}},
                new global::System.Collections.Generic.Dictionary<string, object?>(),
                global::Terminus.ReturnTypeKind.{{returnTypeKindName}},
                handlers,
                isAggregated: true);
            """);

        // Generate interceptor-wrapped aggregation based on return type
        foreach (var statement in BuildAggregatedInterceptorInvocation(facadeInfo, methodGroup))
        {
            yield return statement;
        }
    }

    private static string GetHandlerDescriptorType(ReturnTypeKind returnTypeKind, ITypeSymbol returnType) =>
        returnTypeKind switch
        {
            ReturnTypeKind.Void => "global::Terminus.FacadeVoidHandlerDescriptor",
            ReturnTypeKind.Result => $"global::Terminus.FacadeSyncHandlerDescriptor<{returnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}>",
            ReturnTypeKind.Task or ReturnTypeKind.ValueTask => "global::Terminus.FacadeAsyncVoidHandlerDescriptor",
            ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult =>
                $"global::Terminus.FacadeAsyncHandlerDescriptor<{((INamedTypeSymbol)returnType).TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}>",
            ReturnTypeKind.AsyncEnumerable =>
                $"global::Terminus.FacadeStreamHandlerDescriptor<{((INamedTypeSymbol)returnType).TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}>",
            _ => "global::Terminus.FacadeVoidHandlerDescriptor"
        };

    private static string GetHandlerInvokeCode(ReturnTypeKind returnTypeKind, string invocation) =>
        returnTypeKind switch
        {
            ReturnTypeKind.Void => $"() => {invocation}",
            ReturnTypeKind.Result => $"() => {invocation}",
            ReturnTypeKind.Task or ReturnTypeKind.ValueTask => $"async () => await {invocation}.ConfigureAwait(false)",
            ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult => $"async () => await {invocation}.ConfigureAwait(false)",
            ReturnTypeKind.AsyncEnumerable => $"() => {invocation}",
            _ => $"() => {invocation}"
        };

    private IEnumerable<StatementSyntax> BuildAggregatedInterceptorInvocation(
        FacadeInterfaceInfo facadeInfo,
        AggregatedMethodGroup methodGroup)
    {
        var primaryMethod = methodGroup.PrimaryMethod;

        switch (primaryMethod.ReturnTypeKind)
        {
            case ReturnTypeKind.Void:
                yield return ParseStatement(
                    """
                    ExecuteWithVoidInterceptors(context, handlers =>
                    {
                        foreach (var handler in (handlers ?? context.Handlers).Cast<global::Terminus.FacadeVoidHandlerDescriptor>())
                        {
                            handler.Invoke();
                        }
                    });
                    """);
                break;

            case ReturnTypeKind.Result:
            {
                var returnType = primaryMethod.MethodSymbol.ReturnType
                    .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                yield return ParseStatement(
                    $$"""
                    return ExecuteWithInterceptors<global::System.Collections.Generic.IEnumerable<{{returnType}}>>(
                        context,
                        handlers => QueryInternal(handlers));
                    """);

                yield return ParseStatement(
                    $$"""
                    global::System.Collections.Generic.IEnumerable<{{returnType}}> QueryInternal(global::System.Collections.Generic.IReadOnlyList<global::Terminus.FacadeHandlerDescriptor>? filteredHandlers)
                    {
                        foreach (var handler in (filteredHandlers ?? context.Handlers).Cast<global::Terminus.FacadeSyncHandlerDescriptor<{{returnType}}>>())
                        {
                            yield return handler.Invoke();
                        }
                    }
                    """);
                break;
            }

            case ReturnTypeKind.Task or ReturnTypeKind.ValueTask:
                yield return ParseStatement(
                    """
                    await ExecuteWithAsyncVoidInterceptors(context, async handlers =>
                    {
                        foreach (var handler in (handlers ?? context.Handlers).Cast<global::Terminus.FacadeAsyncVoidHandlerDescriptor>())
                        {
                            await handler.InvokeAsync().ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);
                    """);
                break;

            case ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult:
            {
                var asyncReturnType = ((INamedTypeSymbol)primaryMethod.MethodSymbol.ReturnType).TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                yield return ParseStatement(
                    $$"""
                    return ExecuteWithInterceptorsStream<{{asyncReturnType}}>(
                        context,
                        handlers => QueryInternalAsync(handlers));
                    """);

                yield return ParseStatement(
                    $$"""
                    async global::System.Collections.Generic.IAsyncEnumerable<{{asyncReturnType}}> QueryInternalAsync(global::System.Collections.Generic.IReadOnlyList<global::Terminus.FacadeHandlerDescriptor>? filteredHandlers)
                    {
                        foreach (var handler in (filteredHandlers ?? context.Handlers).Cast<global::Terminus.FacadeAsyncHandlerDescriptor<{{asyncReturnType}}>>())
                        {
                            yield return await handler.InvokeAsync();
                        }
                    }
                    """);
                break;
            }
        }
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
