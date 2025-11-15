using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal readonly record struct AggregatorInterfaceInfo(
    INamedTypeSymbol InterfaceSymbol,
    AttributeData AggregatorAttributeData,
    ImmutableArray<INamedTypeSymbol> EntryPointAttributeTypes,
    ImmutableArray<INamedTypeSymbol> TargetTypes,
    DotnetFeature DotnetFeatures,
    ServiceKind ServiceKind,
    bool Scoped)
{
    public INamedTypeSymbol InterfaceSymbol { get; } = InterfaceSymbol;
    public AttributeData AggregatorAttributeData { get; } = AggregatorAttributeData;
    public ImmutableArray<INamedTypeSymbol> EntryPointAttributeTypes { get; } = EntryPointAttributeTypes;
    public ImmutableArray<INamedTypeSymbol> TargetTypes { get; } = TargetTypes;
    public DotnetFeature DotnetFeatures { get; } = DotnetFeatures;
    public bool Scoped { get; } = Scoped;
    public ServiceKind ServiceKind { get; } = ServiceKind;

    public string GetImplementationClassName() => $"{InterfaceSymbol.Name}_Generated";
    public string GetImplementationClassFullName() => $"{InterfaceSymbol.ToDisplayString()}_Generated";
};
