using System.Collections.Immutable;

namespace Terminus.Generator.Builders;

internal record AggregatorContext(AggregatorInterfaceInfo Aggregator)
{
    public ImmutableArray<CandidateMethodInfo> EntryPointMethodInfos { get; set; } = [];
    public AggregatorInterfaceInfo Aggregator { get; set;  } = Aggregator;
}