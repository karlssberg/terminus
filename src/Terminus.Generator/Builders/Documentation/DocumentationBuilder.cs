using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Documentation;

/// <summary>
/// Builds XML documentation comment trivia for generated facades.
/// </summary>
internal static class DocumentationBuilder
{
    /// <summary>
    /// Builds XML documentation for the facade interface, listing all types being delegated to.
    /// </summary>
    public static SyntaxTriviaList BuildInterfaceDocumentation(ImmutableArray<CandidateMethodInfo> methods)
    {
        var containingTypes = methods
            .Select(m => m.MethodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        if (containingTypes.Count == 0)
            return default;

        if (containingTypes.Count == 1)
        {
            return TriviaList(
                Comment($"/// <summary>"),
                CarriageReturnLineFeed,
                Comment($"/// Facade interface delegating to: <see cref=\"{containingTypes[0]}\"/>"),
                CarriageReturnLineFeed,
                Comment($"/// </summary>"),
                CarriageReturnLineFeed);
        }

        // Multiple types - list one per line
        var triviaList = new System.Collections.Generic.List<SyntaxTrivia>
        {
            Comment("/// <summary>"),
            CarriageReturnLineFeed,
            Comment("/// Facade interface delegating to:<br/>")
        };

        foreach (var type in containingTypes)
        {
            triviaList.Add(CarriageReturnLineFeed);
            triviaList.Add(Comment($"/// <see cref=\"{type}\"/><br/>"));
        }

        triviaList.Add(CarriageReturnLineFeed);
        triviaList.Add(Comment("/// </summary>"));
        triviaList.Add(CarriageReturnLineFeed);

        return TriviaList(triviaList.ToArray());
    }

    /// <summary>
    /// Builds XML documentation for a facade method, including delegation info and original documentation.
    /// </summary>
    public static SyntaxTriviaList BuildMethodDocumentation(
        FacadeInterfaceInfo facadeInfo,
        CandidateMethodInfo methodInfo)
    {
        var crefMethodSignature = BuildMethodCrefSignature(methodInfo.MethodSymbol);

        // If the original method has documentation, include it
        if (!string.IsNullOrWhiteSpace(methodInfo.DocumentationXml))
        {
            var originalDoc = ExtractDocumentationContent(methodInfo.DocumentationXml!);
            if (!string.IsNullOrWhiteSpace(originalDoc))
            {
                return TriviaList(
                    Comment("/// <summary>"),
                    CarriageReturnLineFeed,
                    Comment($"/// Delegates to <see cref=\"{crefMethodSignature}\"/>"),
                    CarriageReturnLineFeed,
                    Comment($"/// <para>{originalDoc}</para>"),
                    CarriageReturnLineFeed,
                    Comment("/// </summary>"),
                    CarriageReturnLineFeed);
            }
        }

        // No original documentation, just show delegation
        return TriviaList(
            Comment("/// <summary>"),
            CarriageReturnLineFeed,
            Comment($"/// Delegates to:<br/>"),
            CarriageReturnLineFeed,
            Comment($"/// <see cref=\"{crefMethodSignature}\"/>"),
            CarriageReturnLineFeed,
            Comment("/// </summary>"),
            CarriageReturnLineFeed);
    }

    /// <summary>
    /// Builds a cref-compatible method signature for XML documentation.
    /// </summary>
    private static string BuildMethodCrefSignature(IMethodSymbol method)
    {
        var containingType = method.ContainingType
            .ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        var methodName = method.Name;

        if (method.Parameters.Length == 0)
            return $"{containingType}.{methodName}";

        var paramTypes = string.Join(", ", method.Parameters
            .Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));

        return $"{containingType}.{methodName}({paramTypes})";
    }

    /// <summary>
    /// Extracts the content from XML documentation comments.
    /// Attempts to parse summary, param, returns, and other common tags.
    /// </summary>
    private static string? ExtractDocumentationContent(string xmlDoc)
    {
        if (string.IsNullOrWhiteSpace(xmlDoc))
            return null;

        // Simple extraction: just get the summary content
        // More sophisticated parsing could handle all XML tags
        var startTag = "<summary>";
        var endTag = "</summary>";
        
        var startIndex = xmlDoc.IndexOf(startTag, StringComparison.Ordinal);
        if (startIndex < 0)
            return null;

        var contentStart = startIndex + startTag.Length;
        var endIndex = xmlDoc.IndexOf(endTag, contentStart, StringComparison.Ordinal);
        if (endIndex < 0)
            return null;

        var content = xmlDoc.Substring(contentStart, endIndex - contentStart).Trim();
        
        // Remove extra whitespace and normalize
        content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ");
        
        return content;
    }
}
