namespace Terminus.Generator.Utilities;

/// <summary>
/// Provides version information for the generator.
/// </summary>
internal static class GeneratorVersion
{
    /// <summary>
    /// Gets the version string to use in the [GeneratedCode] attribute.
    /// </summary>
    public static string Version { get; } = GetVersion();

    private static string GetVersion()
    {
        var assembly = typeof(GeneratorVersion).Assembly;
        var version = assembly.GetName().Version;

        // Return version in format "1.0.0" or fallback to "1.0.0" if version is null
        return version?.ToString(3) ?? "1.0.0";
    }
}
