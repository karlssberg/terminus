namespace Terminus;

/// <summary>
/// Intercepts synchronous void facade method invocations.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface when you need to intercept void methods specifically.
/// For result-returning methods, implement <see cref="ISyncFacadeInterceptor"/>.
/// </para>
/// <para>
/// The <paramref name="next"/> delegate accepts an optional handlers parameter for filtering.
/// Call <c>next()</c> to pass through all handlers, or <c>next(filteredHandlers)</c> to filter.
/// </para>
/// </remarks>
public interface ISyncVoidFacadeInterceptor
{
    /// <summary>
    /// Intercepts a synchronous void facade method invocation.
    /// </summary>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="next">
    /// Delegate to invoke the next interceptor or target method.
    /// Call with no arguments for pass-through, or with filtered handlers list.
    /// </param>
    void Intercept(
        FacadeInvocationContext context,
        FacadeVoidInvocationDelegate next);
}
