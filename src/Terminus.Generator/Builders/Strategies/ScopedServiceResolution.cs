using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Strategies;

/// <summary>
/// Resolves instance methods for scoped facades using lazy-initialized service scopes.
/// </summary>
internal sealed class ScopedServiceResolution : IServiceResolutionStrategy
{
    public bool CanResolve(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo)
    {
        return facadeInfo.Scoped && !methodInfo.MethodSymbol.IsStatic;
    }

    public ExpressionSyntax GetServiceExpression(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo)
    {
        var fullyQualifiedTypeName = methodInfo.MethodSymbol.ContainingType
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Determine which scope to use based on method return type
        var scopeExpression = methodInfo.ReturnTypeKind is ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult or ReturnTypeKind.AsyncEnumerable
            ? "_asyncScope.Value.ServiceProvider"
            : "_syncScope.Value.ServiceProvider";

        return ParseExpression(
            $"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{fullyQualifiedTypeName}>({scopeExpression})");
    }
}
