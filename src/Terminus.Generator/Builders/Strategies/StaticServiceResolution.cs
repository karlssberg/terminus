using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Strategies;

/// <summary>
/// Resolves static methods by returning the fully qualified type name.
/// </summary>
internal sealed class StaticServiceResolution : IServiceResolutionStrategy
{
    public bool CanResolve(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo)
    {
        return methodInfo.MethodSymbol.IsStatic;
    }

    public ExpressionSyntax GetServiceExpression(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo)
    {
        var fullyQualifiedTypeName = methodInfo.MethodSymbol.ContainingType
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return ParseExpression(fullyQualifiedTypeName);
    }
}
