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
                foreach (var statement in methodGroup.Methods.Select(method =>
                    YieldStatement(SyntaxKind.YieldReturnStatement, _invocationBuilder.BuildInvocation(facadeInfo, method))))
                {
                    yield return statement;
                }
                yield break;
            }
            // For async result methods (Task<T>, ValueTask<T>), yield return await each result
            case ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult:
            {
                foreach (var statement in methodGroup.Methods.Select(method =>
                    YieldStatement(SyntaxKind.YieldReturnStatement, AwaitExpression(_invocationBuilder.BuildInvocation(facadeInfo, method)))))
                {
                    yield return statement;
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
