namespace Terminus;

/// <summary>
/// Specifies the lifetime behavior for a generated facade.
/// </summary>
public enum FacadeLifetime
{
    /// <summary>
    /// Non-scoped lifetime. Service instances are resolved per method invocation from the root <see cref="System.IServiceProvider"/>.
    /// This is the default behavior and is appropriate for stateless facades.
    /// </summary>
    Transient = 0,

    /// <summary>
    /// Scoped lifetime. Creates a service scope lazily on first method invocation and reuses it for the lifetime of the facade.
    /// The scope is disposed when the facade is disposed. Appropriate for facades that require per-request or per-operation scope.
    /// Generated facades with scoped lifetime implement <see cref="System.IDisposable"/> and <see cref="System.IAsyncDisposable"/>.
    /// </summary>
    Scoped = 1,

    // Future: Singleton = 2
}
