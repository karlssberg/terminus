namespace Terminus.Generator.Validation;

/// <summary>
/// Utility class for validating C# identifiers.
/// </summary>
internal static class IdentifierValidator
{
    /// <summary>
    /// Checks if a string is a valid C# identifier.
    /// </summary>
    /// <param name="name">The string to validate.</param>
    /// <returns>True if the string is a valid C# identifier; otherwise, false.</returns>
    public static bool IsValidIdentifier(string? name)
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
