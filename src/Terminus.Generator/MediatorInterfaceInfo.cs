using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal readonly record struct MediatorInterfaceInfo(
    INamedTypeSymbol InterfaceSymbol,
    AttributeData AutoGenerateAttributeData,
    INamedTypeSymbol EntryPointAttributeType)
{
    public INamedTypeSymbol InterfaceSymbol { get; } = InterfaceSymbol;
    public AttributeData AutoGenerateAttributeData { get; } = AutoGenerateAttributeData;
    public INamedTypeSymbol EntryPointAttributeType { get; } = EntryPointAttributeType;
    
    public string GetImplementationClassName() => $"{InterfaceSymbol.Name}_Generated";
    public string GetImplementationClassFullName() => $"{InterfaceSymbol.ToDisplayString()}_Generated";
};
