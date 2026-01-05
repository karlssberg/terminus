using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal readonly record struct FacadeInterfaceInfo(
    INamedTypeSymbol InterfaceSymbol,
    ImmutableArray<INamedTypeSymbol> FacadeMethodAttributeTypes,
    ImmutableArray<INamedTypeSymbol> TargetTypes,
    DotnetFeature DotnetFeatures,
    bool Scoped)
{
    public INamedTypeSymbol InterfaceSymbol { get; } = InterfaceSymbol;
    public ImmutableArray<INamedTypeSymbol> FacadeMethodAttributeTypes { get; } = FacadeMethodAttributeTypes;
    public ImmutableArray<INamedTypeSymbol> TargetTypes { get; } = TargetTypes;
    public DotnetFeature DotnetFeatures { get; } = DotnetFeatures;
    public bool Scoped { get; } = Scoped;

    public string GetImplementationClassName() => $"{InterfaceSymbol.Name}_Generated";
}
