using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal readonly record struct AggregatorInterfaceInfo(
    INamedTypeSymbol InterfaceSymbol,
    ImmutableArray<INamedTypeSymbol> EntryPointAttributeTypes,
    ImmutableArray<INamedTypeSymbol> TargetTypes,
    DotnetFeature DotnetFeatures,
    bool Scoped)
{
    public INamedTypeSymbol InterfaceSymbol { get; } = InterfaceSymbol;
    public ImmutableArray<INamedTypeSymbol> EntryPointAttributeTypes { get; } = EntryPointAttributeTypes;
    public ImmutableArray<INamedTypeSymbol> TargetTypes { get; } = TargetTypes;
    public DotnetFeature DotnetFeatures { get; } = DotnetFeatures;
    public bool Scoped { get; } = Scoped;

    public string GetImplementationClassName() => $"{InterfaceSymbol.Name}_Generated";
    public string GetImplementationClassFullName() => $"{InterfaceSymbol.ToDisplayString()}_Generated";
}
