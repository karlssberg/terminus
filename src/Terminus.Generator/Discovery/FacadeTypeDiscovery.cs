using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terminus.Generator.Discovery;

/// <summary>
/// Discovers types (classes/structs/records) that have attributes,
/// and includes all their public methods as facade methods.
/// </summary>
internal sealed class FacadeTypeDiscovery
{
    /// <summary>
    /// Fast syntax-level check to identify candidate types (classes/structs/records with attributes).
    /// </summary>
    public static bool IsCandidateType(SyntaxNode node) =>
        node is TypeDeclarationSyntax { AttributeLists.Count: > 0 } typeDecl &&
        typeDecl is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax;

    /// <summary>
    /// Performs semantic analysis to discover all public methods in types with attributes.
    /// Returns one CandidateMethodInfo per public method, for each attribute on the type.
    /// </summary>
    public static ImmutableArray<CandidateMethodInfo>? DiscoverTypeMethods(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        var typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax, ct);

        if (symbol is not INamedTypeSymbol typeSymbol)
            return null;

        var attributes = typeSymbol.GetAttributes();
        if (attributes.IsEmpty)
            return null;

        // Get all public methods declared on this type (not inherited)
        var publicMethods = typeSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m =>
                m.DeclaredAccessibility == Accessibility.Public &&
                m.MethodKind == MethodKind.Ordinary && // Exclude constructors, operators, property accessors, etc.
                !m.IsGenericMethod) // Filter out generic methods early
            .ToImmutableArray();

        if (publicMethods.IsEmpty)
            return null;

        // For each attribute on the type, create CandidateMethodInfo for each public method
        var result = ImmutableArray.CreateBuilder<CandidateMethodInfo>();

        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass == null)
                continue;

            foreach (var method in publicMethods)
            {
                var returnTypeKind = context.SemanticModel.Compilation.ResolveReturnTypeKind(method);
                var documentationXml = method.GetDocumentationCommentXml(cancellationToken: ct);

                result.Add(new CandidateMethodInfo(method, attribute, returnTypeKind, documentationXml));
            }
        }

        return result.Count > 0 ? result.ToImmutable() : null;
    }

    /// <summary>
    /// Performs semantic analysis to discover all public properties in types with attributes.
    /// Returns one CandidatePropertyInfo per public property, for each attribute on the type.
    /// </summary>
    public static ImmutableArray<CandidatePropertyInfo>? DiscoverTypeProperties(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        var typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax, ct);

        if (symbol is not INamedTypeSymbol typeSymbol)
            return null;

        var attributes = typeSymbol.GetAttributes();
        if (attributes.IsEmpty)
            return null;

        // Get all public properties declared on this type (not inherited, not indexers)
        var publicProperties = typeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p =>
                !p.IsIndexer &&
                (p.GetMethod?.DeclaredAccessibility == Accessibility.Public ||
                 p.SetMethod?.DeclaredAccessibility == Accessibility.Public))
            .ToImmutableArray();

        if (publicProperties.IsEmpty)
            return null;

        // For each attribute on the type, create CandidatePropertyInfo for each public property
        var result = ImmutableArray.CreateBuilder<CandidatePropertyInfo>();

        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass == null)
                continue;

            foreach (var property in publicProperties)
            {
                var documentationXml = property.GetDocumentationCommentXml(cancellationToken: ct);

                result.Add(new CandidatePropertyInfo(property, attribute, documentationXml));
            }
        }

        return result.Count > 0 ? result.ToImmutable() : null;
    }
}
