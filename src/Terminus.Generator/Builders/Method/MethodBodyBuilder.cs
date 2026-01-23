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
        var includeMetadata = facadeInfo.Features.IncludeAttributeMetadata;
        var hasInterceptors = facadeInfo.Features.HasInterceptors;

        // Handle metadata mode with lazy execution (works for single or multiple methods)
        if (includeMetadata)
        {
            foreach (var statement in BuildLazyMetadataMethodBody(facadeInfo, methodGroup))
            {
                yield return statement;
            }
            yield break;
        }

        // For aggregated methods (without metadata), generate yield return statements
        // Note: Interceptors are not supported with aggregation
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

        // Build the context creation statement
        var contextStatement = ParseStatement(
            $$"""
            var context = new global::Terminus.FacadeInvocationContext(
                _serviceProvider,
                typeof({{interfaceName}}).GetMethod("{{methodName}}")!,
                new object?[] { {{argumentsArray}} },
                typeof({{containingTypeName}}),
                new {{attributeTypeName}}(),
                new global::System.Collections.Generic.Dictionary<string, object?>(),
                global::Terminus.ReturnTypeKind.{{returnTypeKindName}});
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

    /// <summary>
    /// Builds method body for lazy metadata mode: yield return (attribute, delegate) tuples.
    /// </summary>
    private IEnumerable<StatementSyntax> BuildLazyMetadataMethodBody(
        FacadeInterfaceInfo facadeInfo,
        AggregatedMethodGroup methodGroup)
    {
        foreach (var method in methodGroup.Methods)
        {
            var attributeExpression = BuildAttributeInstantiation(method);
            var delegateExpression = BuildDelegateExpression(facadeInfo, method);

            var tupleExpression = TupleExpression(
                SeparatedList(new[]
                {
                    Argument(attributeExpression),
                    Argument(delegateExpression)
                }));

            yield return YieldStatement(SyntaxKind.YieldReturnStatement, tupleExpression);
        }
    }

    /// <summary>
    /// Builds a delegate expression (Func or Action lambda) wrapping the method invocation.
    /// </summary>
    private ExpressionSyntax BuildDelegateExpression(
        FacadeInterfaceInfo facadeInfo,
        CandidateMethodInfo method)
    {
        // For lazy execution in metadata mode, we don't want ConfigureAwait(false)
        // The delegate returns Task/ValueTask directly without awaiting
        var invocation = _invocationBuilder.BuildInvocation(facadeInfo, method, includeConfigureAwait: false);
        var returnTypeKind = method.ReturnTypeKind;

        // For void, use Action lambda with block body: () => { invocation; }
        if (returnTypeKind == ReturnTypeKind.Void)
        {
            return ParenthesizedLambdaExpression()
                .WithParameterList(ParameterList())
                .WithBlock(Block(ExpressionStatement(invocation)))
                .NormalizeWhitespace();
        }

        // For all other types, use Func lambda with expression body: () => invocation
        return ParenthesizedLambdaExpression()
            .WithParameterList(ParameterList())
            .WithExpressionBody(invocation)
            .NormalizeWhitespace();
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
