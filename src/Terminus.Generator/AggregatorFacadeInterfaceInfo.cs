using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal readonly record struct AggregatorFacadeInterfaceInfo(
    INamedTypeSymbol InterfaceSymbol,
    AttributeData AggregatorAttributeData,
    ImmutableArray<INamedTypeSymbol> EntryPointAttributeTypes,
    DotnetFeature DotnetFeatures,
    ServiceKind ServiceKind,
    bool Scoped)
{
    public INamedTypeSymbol InterfaceSymbol { get; } = InterfaceSymbol;
    public AttributeData AggregatorAttributeData { get; } = AggregatorAttributeData;
    public ImmutableArray<INamedTypeSymbol> EntryPointAttributeTypes { get; } = EntryPointAttributeTypes;
    public DotnetFeature DotnetFeatures { get; } = DotnetFeatures;
    public bool Scoped { get; } = Scoped;
    public ServiceKind ServiceKind { get; } = ServiceKind;

    public string GetImplementationClassName() => $"{InterfaceSymbol.Name}_Generated";
    public string GetImplementationClassFullName() => $"{InterfaceSymbol.ToDisplayString()}_Generated";
};
