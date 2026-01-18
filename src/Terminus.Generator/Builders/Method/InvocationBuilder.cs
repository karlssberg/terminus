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
    /// <param name="facadeInfo">The facade interface information.</param>
    /// <param name="methodInfo">The method information.</param>
    /// <param name="includeConfigureAwait">Whether to append ConfigureAwait(false) for async methods. Set to false when building delegates for lazy execution.</param>
    public InvocationExpressionSyntax BuildInvocation(
        FacadeInterfaceInfo facadeInfo,
        CandidateMethodInfo methodInfo,
        bool includeConfigureAwait = true)
    {
        // Get service/type expression
        var instanceExpression = serviceResolution.GetServiceExpression(facadeInfo, methodInfo);

        // Build method access
        var methodName = methodInfo.MethodSymbol.Name;
        ExpressionSyntax methodAccess;

        if (methodInfo.MethodSymbol.IsGenericMethod)
        {
            var typeArguments = TypeArgumentList(SeparatedList(
                methodInfo.MethodSymbol.TypeParameters.Select(tp =>
                    ParseTypeName(tp.Name.EscapeIdentifier()))));

            methodAccess = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                instanceExpression,
                GenericName(Identifier(methodName))
                    .WithTypeArgumentList(typeArguments));
        }
        else
        {
            methodAccess = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                instanceExpression,
                IdentifierName(methodName));
        }

        // Build argument list
        var argumentList = ArgumentList(SeparatedList(
            methodInfo.MethodSymbol.Parameters.Select(p =>
                Argument(IdentifierName(p.Name.EscapeIdentifier())))));

        var invocationExpression = InvocationExpression(methodAccess, argumentList);

        // For async Task/ValueTask/Task<T>/ValueTask<T>, append ConfigureAwait(false)
        // Skip this when building delegates for lazy execution (metadata mode)
        var isAsyncTask = methodInfo.ReturnTypeKind is ReturnTypeKind.Task
                                                    or ReturnTypeKind.TaskWithResult
                                                    or ReturnTypeKind.ValueTask
                                                    or ReturnTypeKind.ValueTaskWithResult;

        if (isAsyncTask && includeConfigureAwait)
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
