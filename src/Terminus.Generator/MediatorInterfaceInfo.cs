using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal readonly record struct MediatorInterfaceInfo(
    INamedTypeSymbol InterfaceSymbol,
    AttributeData MediatorAttributeData,
    INamedTypeSymbol EntryPointAttributeType)
{
    public INamedTypeSymbol InterfaceSymbol { get; } = InterfaceSymbol;
    public AttributeData MediatorAttributeData { get; } = MediatorAttributeData;
    public INamedTypeSymbol EntryPointAttributeType { get; } = EntryPointAttributeType;
};
