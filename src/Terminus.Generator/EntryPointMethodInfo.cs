using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal readonly record struct EntryPointMethodInfo(
    IMethodSymbol MethodSymbol,
    AttributeData AttributeData)
{
    public IMethodSymbol MethodSymbol { get; } = MethodSymbol;
    public AttributeData AttributeData { get; } = AttributeData;
};