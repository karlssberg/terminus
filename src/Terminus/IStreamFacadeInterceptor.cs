using System.Collections.Generic;

namespace Terminus;

/// <summary>
/// Intercepts streaming facade method invocations (IAsyncEnumerable&lt;T&gt; returning methods).
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface when you need to intercept streaming methods.
/// For sync and async methods, implement the respective interceptor interfaces.
/// </para>
/// <para>
/// The <paramref name="next"/> delegate accepts an optional handlers parameter for filtering.
/// Call <c>next()</c> to pass through all handlers, or <c>next(filteredHandlers)</c> to filter.
/// </para>
/// </remarks>
public interface IStreamFacadeInterceptor
{
    /// <summary>
    /// Intercepts a streaming facade method invocation (IAsyncEnumerable&lt;T&gt;).
    /// </summary>
    /// <typeparam name="TItem">The type of items in the stream.</typeparam>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="next">
    /// Delegate to invoke the next interceptor or target method stream.
    /// Call with no arguments for pass-through, or with filtered handlers list.
    /// </param>
    /// <returns>An async enumerable of items.</returns>
    IAsyncEnumerable<TItem> InterceptStream<TItem>(
        FacadeInvocationContext context,
        FacadeStreamInvocationDelegate<TItem> next);
}
