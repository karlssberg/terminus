using System.Collections.Immutable;

namespace Terminus.Generator.Builders;

internal record AggregatorContext(AggregatorInterfaceInfo Aggregator)
{
    public ImmutableArray<EntryPointMethodInfo> EntryPointMethodInfos { get; set; } = [];
    public AggregatorInterfaceInfo Aggregator { get; set;  } = Aggregator;
}