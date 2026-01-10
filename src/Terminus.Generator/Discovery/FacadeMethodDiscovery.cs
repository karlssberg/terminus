using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terminus.Generator.Discovery;

/// <summary>
/// Discovers methods that have attributes and could be facade methods.
/// </summary>
internal sealed class FacadeMethodDiscovery
{
    /// <summary>
    /// Fast syntax-level check to identify candidate methods.
    /// </summary>
    public static bool IsCandidateMethod(SyntaxNode node) =>
        node is MethodDeclarationSyntax;

    /// <summary>
    /// Performs semantic analysis to discover methods with attributes.
    /// Returns one CandidateMethodInfo per attribute on the method.
    /// </summary>
    public static ImmutableArray<CandidateMethodInfo>? DiscoverMethods(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax, ct);

        if (symbol is not IMethodSymbol methodSymbol)
            return null;

        var attributes = methodSymbol.GetAttributes();
        if (attributes.IsEmpty)
            return null;

        var returnTypeKind = context.SemanticModel.Compilation.ResolveReturnTypeKind(methodSymbol);
        var documentationXml = methodSymbol.GetDocumentationCommentXml(cancellationToken: ct);

        // Return one CandidateMethodInfo per attribute on the method
        return [
            ..attributes
                .Where(attr => attr.AttributeClass != null)
                .Select(attr => new CandidateMethodInfo(methodSymbol, attr, returnTypeKind, documentationXml))
        ];
    }
}
