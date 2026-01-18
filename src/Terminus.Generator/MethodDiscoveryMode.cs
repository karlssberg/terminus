namespace Terminus.Generator;

/// <summary>
/// Specifies which assemblies should be scanned when discovering facade methods.
/// This enum mirrors <c>Terminus.MethodDiscoveryMode</c> for use within the generator.
/// </summary>
internal enum MethodDiscoveryMode
{
    /// <summary>
    /// Only discover methods from the current compilation.
    /// </summary>
    None = 0,

    /// <summary>
    /// Discover methods from directly referenced assemblies only.
    /// </summary>
    ReferencedAssemblies = 1,

    /// <summary>
    /// Discover methods from all referenced assemblies, including transitive dependencies.
    /// </summary>
    TransitiveAssemblies = 2
}
