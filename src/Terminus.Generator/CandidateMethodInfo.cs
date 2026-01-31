using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal readonly record struct CandidateMethodInfo(
    IMethodSymbol MethodSymbol,
    AttributeData AttributeData,
    ReturnTypeKind ReturnTypeKind,
    string? DocumentationXml,
    INamedTypeSymbol? OriginalOpenGenericType = null,
    ImmutableArray<ITypeSymbol>? TypeArguments = null)
{
    public IMethodSymbol MethodSymbol { get; } = MethodSymbol;
    public AttributeData AttributeData { get; } = AttributeData;
    public ReturnTypeKind ReturnTypeKind { get; } = ReturnTypeKind;
    public string? DocumentationXml { get; } = DocumentationXml;

    /// <summary>
    /// If this method comes from a closed generic type, this contains the original open generic type definition.
    /// </summary>
    public INamedTypeSymbol? OriginalOpenGenericType { get; } = OriginalOpenGenericType;

    /// <summary>
    /// If this method comes from a closed generic type, this contains the type arguments used to construct it.
    /// </summary>
    public ImmutableArray<ITypeSymbol>? TypeArguments { get; } = TypeArguments;

    /// <summary>
    /// Returns true if this method originated from an open generic type.
    /// </summary>
    public bool IsFromClosedGeneric => OriginalOpenGenericType != null;
};