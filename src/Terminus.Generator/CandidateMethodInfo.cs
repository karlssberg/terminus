using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal readonly record struct CandidateMethodInfo(
    IMethodSymbol MethodSymbol,
    AttributeData AttributeData,
    ReturnTypeKind ReturnTypeKind)
{
    public IMethodSymbol MethodSymbol { get; } = MethodSymbol;
    public AttributeData AttributeData { get; } = AttributeData;
    public ReturnTypeKind ReturnTypeKind { get; } = ReturnTypeKind;
};