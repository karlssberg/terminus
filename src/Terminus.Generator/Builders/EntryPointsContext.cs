using System.Collections.Immutable;

namespace Terminus.Generator.Builders;

internal record AggregatorContext(AggregatorFacadeInterfaceInfo Facade)
{
    public ImmutableArray<EntryPointMethodInfo> EntryPointMethodInfos { get; set; } = [];
    public AggregatorFacadeInterfaceInfo Facade { get; set;  } = Facade;
}