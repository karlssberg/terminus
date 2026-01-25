using System.Threading.Tasks;

namespace Terminus;

/// <summary>
/// Intercepts asynchronous void facade method invocations (Task or ValueTask methods).
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface when you need to intercept async void methods specifically.
/// For result-returning async methods, implement <see cref="IAsyncFacadeInterceptor"/>.
/// </para>
/// <para>
/// The <paramref name="next"/> delegate accepts an optional handlers parameter for filtering.
/// Call <c>next()</c> to pass through all handlers, or <c>next(filteredHandlers)</c> to filter.
/// </para>
/// </remarks>
public interface IAsyncVoidFacadeInterceptor
{
    /// <summary>
    /// Intercepts an asynchronous void facade method invocation (Task or ValueTask).
    /// </summary>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="next">
    /// Delegate to invoke the next interceptor or target method.
    /// Call with no arguments for pass-through, or with filtered handlers list.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InterceptAsync(
        FacadeInvocationContext context,
        FacadeAsyncVoidInvocationDelegate next);
}
