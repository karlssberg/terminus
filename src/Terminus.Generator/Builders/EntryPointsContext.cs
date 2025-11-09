using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Terminus.Generator.Builders;

internal record EntryPointsContext(INamedTypeSymbol EntryPointAttributeType)
{
    public INamedTypeSymbol EntryPointAttributeType { get; } = EntryPointAttributeType;
    public ImmutableArray<EntryPointMethodInfo> EntryPointMethodInfos { get; set; } = [];
    public ImmutableArray<MediatorInterfaceInfo> Mediators { get; set;  } = [];
}