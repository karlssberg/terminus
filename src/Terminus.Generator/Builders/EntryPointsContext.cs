using System.Collections.Immutable;
using Terminus.Generator.Grouping;

namespace Terminus.Generator.Builders;

/// <summary>
/// Immutable context containing all information needed to generate a facade implementation.
/// </summary>
internal sealed record FacadeGenerationContext(
    FacadeInterfaceInfo Facade,
    ImmutableArray<AggregatedMethodGroup> MethodGroups,
    ImmutableArray<CandidatePropertyInfo> Properties)
{
    /// <summary>
    /// Creates a new facade generation context.
    /// </summary>
    public static FacadeGenerationContext Create(
        FacadeInterfaceInfo facade,
        ImmutableArray<CandidateMethodInfo> facadeMethodMethodInfos,
        ImmutableArray<CandidatePropertyInfo> facadePropertyInfos = default)
    {
        var methodGroups = MethodSignatureGrouper.GroupBySignature(facade, facadeMethodMethodInfos);
        var properties = facadePropertyInfos.IsDefault ? ImmutableArray<CandidatePropertyInfo>.Empty : facadePropertyInfos;
        return new FacadeGenerationContext(facade, methodGroups, properties);
    }

    /// <summary>
    /// Creates a new facade generation context with pre-grouped methods.
    /// </summary>
    public static FacadeGenerationContext CreateWithGroups(
        FacadeInterfaceInfo facade,
        ImmutableArray<AggregatedMethodGroup> methodGroups,
        ImmutableArray<CandidatePropertyInfo> facadePropertyInfos = default)
    {
        var properties = facadePropertyInfos.IsDefault ? ImmutableArray<CandidatePropertyInfo>.Empty : facadePropertyInfos;
        return new FacadeGenerationContext(facade, methodGroups, properties);
    }
}