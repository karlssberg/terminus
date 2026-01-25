using System.Collections.Generic;

namespace Terminus;

/// <summary>
/// Intercepts streaming facade method invocations (IAsyncEnumerable&lt;T&gt; returning methods).
/// </summary>
/// <remarks>
/// Implement this interface when you only need to intercept streaming methods.
/// For full interception including sync and async, implement <see cref="IFacadeInterceptor"/>
/// or inherit from <see cref="FacadeInterceptor"/>.
/// </remarks>
public interface IStreamFacadeInterceptor
{
    /// <summary>
    /// Intercepts a streaming facade method invocation (IAsyncEnumerable&lt;T&gt;).
    /// </summary>
    /// <typeparam name="TItem">The type of items in the stream.</typeparam>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="next">Delegate to invoke the next interceptor or target method stream.</param>
    /// <returns>An async enumerable of items.</returns>
    IAsyncEnumerable<TItem> InterceptStream<TItem>(
        FacadeInvocationContext context,
        FacadeStreamInvocationDelegate<TItem> next);
}
