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
        CandidateMethodInfo methodInfo)
    {
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
            ReturnTypeKind.AsyncEnumerable when facadeInfo.Scoped =>
                BuildAsyncEnumerableProxyStatement(facadeInfo, methodInfo),
            _ => ReturnStatement(invocationExpression)
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
