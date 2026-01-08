using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Strategies;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Method;

/// <summary>
/// Builds method invocation expressions with appropriate awaiting and ConfigureAwait.
/// </summary>
internal sealed class InvocationBuilder(IServiceResolutionStrategy serviceResolution)
{
    /// <summary>
    /// Builds the complete method invocation expression.
    /// </summary>
    public InvocationExpressionSyntax BuildInvocation(
        FacadeInterfaceInfo facadeInfo,
        CandidateMethodInfo methodInfo)
    {
        // Get service/type expression
        var instanceExpression = serviceResolution.GetServiceExpression(facadeInfo, methodInfo);

        // Build method access
        var methodAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            instanceExpression,
            IdentifierName(methodInfo.MethodSymbol.Name));

        // Build argument list
        var argumentList = ArgumentList(SeparatedList(
            methodInfo.MethodSymbol.Parameters.Select(p => Argument(IdentifierName(p.Name)))));

        var invocationExpression = InvocationExpression(methodAccess, argumentList);

        // For async Task/Task<T>, append ConfigureAwait(false)
        var isAsyncTask = methodInfo.ReturnTypeKind is ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult;
        if (isAsyncTask)
        {
            invocationExpression = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    invocationExpression,
                    IdentifierName("ConfigureAwait")))
                .WithArgumentList(ArgumentList(
                    SingletonSeparatedList(
                        Argument(LiteralExpression(SyntaxKind.FalseLiteralExpression)))));
        }

        return invocationExpression;
    }
}
