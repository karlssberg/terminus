using System.Collections.Immutable;

namespace Terminus.Generator;

/// <summary>
/// Represents a group of methods that share the same signature and should be aggregated into a single facade method.
/// </summary>
internal sealed record AggregatedMethodGroup(
    /// <summary>
    /// All methods in this group (min 1, typically >1 when aggregating).
    /// </summary>
    ImmutableArray<CandidateMethodInfo> Methods)
{
    /// <summary>
    /// Gets whether this group contains multiple methods that need aggregation.
    /// </summary>
    public bool RequiresAggregation => Methods.Length > 1;

    /// <summary>
    /// Gets the primary method (used for signature generation).
    /// </summary>
    public CandidateMethodInfo PrimaryMethod => Methods[0];
}
