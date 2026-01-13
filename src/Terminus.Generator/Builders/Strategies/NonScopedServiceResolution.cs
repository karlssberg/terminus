using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Strategies;

/// <summary>
/// Resolves instance methods for non-scoped facades using the IServiceProvider field.
/// </summary>
internal sealed class NonScopedServiceResolution : IServiceResolutionStrategy
{
    public bool CanResolve(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo)
    {
        return !facadeInfo.Features.IsScoped && !methodInfo.MethodSymbol.IsStatic;
    }

    public ExpressionSyntax GetServiceExpression(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo)
    {
        var fullyQualifiedTypeName = methodInfo.MethodSymbol.ContainingType
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return ParseExpression(
            $"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{fullyQualifiedTypeName}>(_serviceProvider)");
    }
}
