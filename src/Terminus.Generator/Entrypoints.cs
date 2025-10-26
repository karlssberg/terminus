using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal readonly record struct AttributeInfo(AttributeData AttributeData)
{
    public AttributeData AttributeData { get; } = AttributeData;
}