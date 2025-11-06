using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal static class SymbolExtensions
{
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
}