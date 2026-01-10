using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal readonly record struct CandidateMethodInfo(
    IMethodSymbol MethodSymbol,
    AttributeData AttributeData,
    ReturnTypeKind ReturnTypeKind,
    string? DocumentationXml)
{
    public IMethodSymbol MethodSymbol { get; } = MethodSymbol;
    public AttributeData AttributeData { get; } = AttributeData;
    public ReturnTypeKind ReturnTypeKind { get; } = ReturnTypeKind;
    public string? DocumentationXml { get; } = DocumentationXml;
};