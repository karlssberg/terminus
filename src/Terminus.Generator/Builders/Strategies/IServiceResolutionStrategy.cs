using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terminus.Generator.Builders.Strategies;

/// <summary>
/// Strategy for resolving service instances based on method characteristics and facade configuration.
/// </summary>
internal interface IServiceResolutionStrategy
{
    /// <summary>
    /// Determines if this strategy can handle the given method.
    /// </summary>
    bool CanResolve(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo);

    /// <summary>
    /// Gets the expression that resolves the service instance or type for method invocation.
    /// </summary>
    ExpressionSyntax GetServiceExpression(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo);
}
