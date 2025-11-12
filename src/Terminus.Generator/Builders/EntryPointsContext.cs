using System.Collections.Immutable;

namespace Terminus.Generator.Builders;

internal record FacadeContext(FacadeInterfaceInfo Facade)
{
    public ImmutableArray<EntryPointMethodInfo> EntryPointMethodInfos { get; set; } = [];
    public FacadeInterfaceInfo Facade { get; set;  } = Facade;
}