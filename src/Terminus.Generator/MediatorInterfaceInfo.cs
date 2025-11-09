using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal readonly record struct MediatorInterfaceInfo(
    INamedTypeSymbol InterfaceSymbol,
    AttributeData AutoGenerateAttributeData,
    INamedTypeSymbol EntryPointAttributeType,
    DotnetFeature DotnetFeatures)
{
    public INamedTypeSymbol InterfaceSymbol { get; } = InterfaceSymbol;
    public AttributeData AutoGenerateAttributeData { get; } = AutoGenerateAttributeData;
    public INamedTypeSymbol EntryPointAttributeType { get; } = EntryPointAttributeType;
    public DotnetFeature DotnetFeatures { get; } = DotnetFeatures;

    public string GetImplementationClassName() => $"{InterfaceSymbol.Name}_Generated";
    public string GetImplementationClassFullName() => $"{InterfaceSymbol.ToDisplayString()}_Generated";
};
