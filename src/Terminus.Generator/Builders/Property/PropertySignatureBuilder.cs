using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Documentation;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Terminus.Generator.Builders.Property;

/// <summary>
/// Builds property signatures for interface declarations.
/// </summary>
internal static class PropertySignatureBuilder
{
    /// <summary>
    /// Builds a property declaration with signature only (no body) for the interface.
    /// </summary>
    public static PropertyDeclarationSyntax BuildInterfaceProperty(
        FacadeInterfaceInfo facadeInfo,
        CandidatePropertyInfo propertyInfo)
    {
        var propertySymbol = propertyInfo.PropertySymbol;
        var propertyTypeSyntax = ParseTypeName(propertySymbol.Type.ToDisplayString(FullyQualifiedFormat));
        var documentation = DocumentationBuilder.BuildPropertyDocumentation(facadeInfo, propertyInfo);

        var accessors = new List<AccessorDeclarationSyntax>();

        // Add getter if the property has a public getter
        if (propertyInfo.HasGetter)
        {
            accessors.Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
        }

        // Add setter if the property has a public setter (including init-only)
        // Init-only setters are converted to regular setters for interface compatibility
        if (propertyInfo.HasSetter)
        {
            accessors.Add(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
        }

        return PropertyDeclaration(propertyTypeSyntax, Identifier(propertySymbol.Name))
            .WithLeadingTrivia(documentation)
            .WithAccessorList(AccessorList(List(accessors)));
    }
}
