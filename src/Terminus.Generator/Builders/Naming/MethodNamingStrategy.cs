namespace Terminus.Generator.Builders.Naming;

/// <summary>
/// Determines the method name for a facade method based on the facade configuration and method return type.
/// </summary>
internal static class MethodNamingStrategy
{
    /// <summary>
    /// Gets the appropriate method name based on facade configuration and return type.
    /// </summary>
    public static string GetMethodName(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo candidate)
    {
        return candidate.ReturnTypeKind switch
        {
            ReturnTypeKind.Void => GetValidMethodNameOrNull(facadeInfo.Features.CommandName) ?? candidate.MethodSymbol.Name,
            ReturnTypeKind.Result => GetValidMethodNameOrNull(facadeInfo.Features.QueryName) ?? candidate.MethodSymbol.Name,
            ReturnTypeKind.Task => GetValidMethodNameOrNull(facadeInfo.Features.AsyncCommandName) ?? candidate.MethodSymbol.Name,
            ReturnTypeKind.ValueTask => GetValidMethodNameOrNull(facadeInfo.Features.AsyncCommandName) ?? candidate.MethodSymbol.Name,
            ReturnTypeKind.TaskWithResult => GetValidMethodNameOrNull(facadeInfo.Features.AsyncQueryName) ?? candidate.MethodSymbol.Name,
            ReturnTypeKind.ValueTaskWithResult => GetValidMethodNameOrNull(facadeInfo.Features.AsyncQueryName) ?? candidate.MethodSymbol.Name,
            ReturnTypeKind.AsyncEnumerable => GetValidMethodNameOrNull(facadeInfo.Features.AsyncStreamName) ?? candidate.MethodSymbol.Name,
            _ => candidate.MethodSymbol.Name
        };
    }

    /// <summary>
    /// Returns the input string if it's a valid method name, otherwise returns null.
    /// </summary>
    private static string? GetValidMethodNameOrNull(string? name)
    {
        // Check if it's a valid C# identifier
        return IsValidIdentifier(name) ? name : null;
    }

    /// <summary>
    /// Checks if a string is a valid C# identifier.
    /// </summary>
    private static bool IsValidIdentifier(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // First character must be a letter or underscore
        if (!char.IsLetter(name![0]) && name[0] != '_')
            return false;

        // Remaining characters must be letters, digits, or underscores
        return name.All(ch => char.IsLetterOrDigit(ch) || ch == '_');
    }
}
