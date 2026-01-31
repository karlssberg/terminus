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
    /// <param name="facadeInfo">The facade interface information.</param>
    /// <param name="methodInfo">The method information.</param>
    /// <param name="isAggregation">Whether this is being called in an aggregation context where multiple implementations should be resolved.</param>
    ExpressionSyntax GetServiceExpression(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo, bool isAggregation = false);
}
