using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Terminus.Generator;

internal static class SymbolExtensions
{
    internal static T? GetNamedArgument<T>(this AttributeData attributeData, string argName) =>
        attributeData.NamedArguments
            .Where(namedArg => namedArg.Key == argName)
            .Select(namedArg => namedArg.Value.Value)
            .OfType<T>()
            .FirstOrDefault();

    internal static string ToIdentifierString(this INamedTypeSymbol symbol) => symbol.ToDisplayString()
        .ToIdentifierString();
    
    internal static string ToVariableIdentifierString(this INamedTypeSymbol symbol) => symbol.ToDisplayString()
        .ToIdentifierString()
        .ToCamelCase();
    
    private static string ToIdentifierString(this string value) => value
        .Replace(".", "_")
        .Replace("<", "__")
        .Replace(">", "__");
    
    private static string ToCamelCase(this string value) =>
        string.Concat(value.Substring(0, 1).ToLower(), value.Substring(1));

    internal static string EscapeIdentifier(this string identifier) => 
        SyntaxFacts.IsReservedKeyword(SyntaxFacts.GetKeywordKind(identifier)) 
            ? "@" + identifier 
            : identifier;
}