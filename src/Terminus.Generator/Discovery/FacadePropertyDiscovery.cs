using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terminus.Generator.Discovery;

/// <summary>
/// Discovers properties marked with facade attributes.
/// </summary>
internal sealed class FacadePropertyDiscovery
{
    /// <summary>
    /// Fast syntax-level check to identify candidate properties.
    /// </summary>
    public static bool IsCandidateProperty(SyntaxNode node) =>
        node is PropertyDeclarationSyntax { AttributeLists.Count: > 0 };

    /// <summary>
    /// Performs semantic analysis to discover properties with attributes.
    /// Returns one CandidatePropertyInfo per attribute on the property.
    /// </summary>
    public static ImmutableArray<CandidatePropertyInfo>? DiscoverProperties(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        var propertySyntax = (PropertyDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(propertySyntax, ct);

        if (symbol is not IPropertySymbol propertySymbol)
            return null;

        // Skip indexers
        if (propertySymbol.IsIndexer)
            return null;

        // Skip if the property doesn't have a public getter or setter
        if (propertySymbol.GetMethod?.DeclaredAccessibility != Accessibility.Public &&
            propertySymbol.SetMethod?.DeclaredAccessibility != Accessibility.Public)
            return null;

        var attributes = propertySymbol.GetAttributes();
        if (attributes.IsEmpty)
            return null;

        var documentationXml = propertySymbol.GetDocumentationCommentXml(cancellationToken: ct);

        // Return one CandidatePropertyInfo per attribute on the property
        return [
            ..attributes
                .Where(attr => attr.AttributeClass != null)
                .Select(attr => new CandidatePropertyInfo(propertySymbol, attr, documentationXml))
        ];
    }
}
