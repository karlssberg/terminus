namespace Terminus;

/// <summary>
/// Specifies which assemblies should be scanned when discovering facade methods.
/// </summary>
public enum MethodDiscoveryMode
{
    /// <summary>
    /// Only discover methods from the current compilation.
    /// This is the default behavior - no referenced assemblies are scanned.
    /// </summary>
    None = 0,

    /// <summary>
    /// Discover methods from directly referenced assemblies only.
    /// Scans assemblies that are explicitly referenced by the current project,
    /// but not their transitive dependencies.
    /// </summary>
    ReferencedAssemblies = 1,

    /// <summary>
    /// Discover methods from all referenced assemblies, including transitive dependencies.
    /// Scans all assemblies available to the compilation, including those that are
    /// indirectly referenced through the dependency graph.
    /// </summary>
    TransitiveAssemblies = 2
}
