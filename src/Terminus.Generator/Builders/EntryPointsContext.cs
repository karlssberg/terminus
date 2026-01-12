using System.Collections.Immutable;
using Terminus.Generator.Grouping;

namespace Terminus.Generator.Builders;

/// <summary>
/// Immutable context containing all information needed to generate a facade implementation.
/// </summary>
internal sealed record FacadeGenerationContext(
    FacadeInterfaceInfo Facade,
    ImmutableArray<AggregatedMethodGroup> MethodGroups)
{
    /// <summary>
    /// Creates a new facade generation context.
    /// </summary>
    public static FacadeGenerationContext Create(
        FacadeInterfaceInfo facade,
        ImmutableArray<CandidateMethodInfo> facadeMethodMethodInfos)
    {
        var methodGroups = MethodSignatureGrouper.GroupBySignature(facade, facadeMethodMethodInfos);
        return new FacadeGenerationContext(facade, methodGroups);
    }
}