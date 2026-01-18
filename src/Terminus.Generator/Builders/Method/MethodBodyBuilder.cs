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
        // For aggregated methods, generate yield return statements
        if (methodGroup.RequiresAggregation)
        {
            foreach (var statement in BuildAggregatedMethodBody(facadeInfo, methodGroup))
            {
                yield return statement;
            }
            yield break;
        }

        // For single methods, use existing logic
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

    private IEnumerable<StatementSyntax> BuildAggregatedMethodBody(
        FacadeInterfaceInfo facadeInfo,
        AggregatedMethodGroup methodGroup)
    {
        var primaryMethod = methodGroup.PrimaryMethod;
        var returnTypeKind = primaryMethod.ReturnTypeKind;
        var includeMetadata = facadeInfo.Features.IncludeAttributeMetadata;

        switch (returnTypeKind)
        {
            // For void methods, just execute all handlers in sequence (no metadata for void)
            case ReturnTypeKind.Void:
            {
                foreach (var statement in methodGroup.Methods.Select(method =>
                    ExpressionStatement(_invocationBuilder.BuildInvocation(facadeInfo, method))))
                {
                    yield return statement;
                }
                yield break;
            }
            // For result methods (T), yield return each result (with optional metadata tuple)
            case ReturnTypeKind.Result:
            {
                foreach (var method in methodGroup.Methods)
                {
                    var invocation = _invocationBuilder.BuildInvocation(facadeInfo, method);

                    if (includeMetadata)
                    {
                        var attributeExpression = BuildAttributeInstantiation(method);
                        var tupleExpression = TupleExpression(
                            SeparatedList(new[]
                            {
                                Argument(attributeExpression),
                                Argument(invocation)
                            }));
                        yield return YieldStatement(SyntaxKind.YieldReturnStatement, tupleExpression);
                    }
                    else
                    {
                        yield return YieldStatement(SyntaxKind.YieldReturnStatement, invocation);
                    }
                }
                yield break;
            }
            // For async result methods (Task<T>, ValueTask<T>), yield return await each result (with optional metadata tuple)
            case ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult:
            {
                foreach (var method in methodGroup.Methods)
                {
                    var invocation = _invocationBuilder.BuildInvocation(facadeInfo, method);
                    var awaitedInvocation = AwaitExpression(invocation);

                    if (includeMetadata)
                    {
                        var attributeExpression = BuildAttributeInstantiation(method);
                        var tupleExpression = TupleExpression(
                            SeparatedList(new[]
                            {
                                Argument(attributeExpression),
                                Argument(awaitedInvocation)
                            }));
                        yield return YieldStatement(SyntaxKind.YieldReturnStatement, tupleExpression);
                    }
                    else
                    {
                        yield return YieldStatement(SyntaxKind.YieldReturnStatement, awaitedInvocation);
                    }
                }
                yield break;
            }
            // For Task/ValueTask without results, await all (no metadata for void-returning tasks)
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
