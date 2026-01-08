using System.Collections.Immutable;

namespace Terminus.Generator.Builders;

/// <summary>
/// Immutable context containing all information needed to generate a facade implementation.
/// </summary>
internal sealed record FacadeGenerationContext(
    FacadeInterfaceInfo Facade,
    ImmutableArray<CandidateMethodInfo> FacadeMethodMethodInfos)
{
    /// <summary>
    /// Creates a new facade generation context.
    /// </summary>
    public static FacadeGenerationContext Create(
        FacadeInterfaceInfo facade,
        ImmutableArray<CandidateMethodInfo> facadeMethodMethodInfos)
    {
        return new FacadeGenerationContext(facade, facadeMethodMethodInfos);
    }
}