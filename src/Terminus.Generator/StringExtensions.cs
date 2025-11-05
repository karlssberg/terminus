namespace Terminus.Generator;

public static class StringExtensions
{
    public static string EscapeIdentifierName(this string value) => value
        .Replace(".", "_")
        .Replace("<", "__")
        .Replace(">", "__");
}