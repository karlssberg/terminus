using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

/// <summary>
/// Represents a candidate property discovered from the source code.
/// </summary>
internal readonly record struct CandidatePropertyInfo(
    IPropertySymbol PropertySymbol,
    AttributeData AttributeData,
    string? DocumentationXml)
{
    /// <summary>
    /// The property symbol containing the property metadata.
    /// </summary>
    public IPropertySymbol PropertySymbol { get; } = PropertySymbol;

    /// <summary>
    /// The attribute data that identifies this as a facade property.
    /// </summary>
    public AttributeData AttributeData { get; } = AttributeData;

    /// <summary>
    /// The XML documentation for this property.
    /// </summary>
    public string? DocumentationXml { get; } = DocumentationXml;

    /// <summary>
    /// Gets whether this property has a public getter.
    /// </summary>
    public bool HasGetter => PropertySymbol.GetMethod?.DeclaredAccessibility == Accessibility.Public;

    /// <summary>
    /// Gets whether this property has a public setter (excluding init-only setters).
    /// Init-only setters cannot be assigned outside of object initializers, so they are
    /// treated as read-only from the facade's perspective.
    /// </summary>
    public bool HasSetter => PropertySymbol.SetMethod?.DeclaredAccessibility == Accessibility.Public
                             && !IsInitOnly;

    /// <summary>
    /// Gets whether this property has an init-only setter.
    /// </summary>
    public bool IsInitOnly => PropertySymbol.SetMethod?.IsInitOnly ?? false;
}
