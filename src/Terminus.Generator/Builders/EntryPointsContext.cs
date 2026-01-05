using System.Collections.Immutable;

namespace Terminus.Generator.Builders;

internal record AggregatorContext(FacadeInterfaceInfo Facade)
{
    public ImmutableArray<CandidateMethodInfo> FacadeMethodMethodInfos { get; set; } = [];
    public FacadeInterfaceInfo Facade { get; set;  } = Facade;
}