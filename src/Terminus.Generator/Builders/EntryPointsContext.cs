using System.Collections.Immutable;

namespace Terminus.Generator.Builders;

internal record AggregatorContext(AggregatorInterfaceInfo Facade)
{
    public ImmutableArray<EntryPointMethodInfo> EntryPointMethodInfos { get; set; } = [];
    public AggregatorInterfaceInfo Facade { get; set;  } = Facade;
}