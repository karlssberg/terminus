using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal readonly record struct FacadeInterfaceInfo(
    INamedTypeSymbol InterfaceSymbol,
    AttributeData FacadeAttributeData,
    ImmutableArray<INamedTypeSymbol> EntryPointAttributeTypes,
    DotnetFeature DotnetFeatures,
    bool Scoped)
{
    public INamedTypeSymbol InterfaceSymbol { get; } = InterfaceSymbol;
    public AttributeData FacadeAttributeData { get; } = FacadeAttributeData;
    public ImmutableArray<INamedTypeSymbol> EntryPointAttributeTypes { get; } = EntryPointAttributeTypes;
    public DotnetFeature DotnetFeatures { get; } = DotnetFeatures;
    public bool Scoped { get; } = Scoped;

    public string GetImplementationClassName() => $"{InterfaceSymbol.Name}_Generated";
    public string GetImplementationClassFullName() => $"{InterfaceSymbol.ToDisplayString()}_Generated";
};
