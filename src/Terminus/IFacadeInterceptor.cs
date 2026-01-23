using System.Collections.Generic;
using System.Threading.Tasks;

namespace Terminus;

/// <summary>
/// Intercepts facade method invocations for cross-cutting concerns.
/// Interceptors execute in a chain, allowing multiple concerns to be composed.
/// Supports synchronous, asynchronous, and streaming methods.
/// </summary>
/// <remarks>
/// Most users should inherit from <see cref="FacadeInterceptor"/> instead of implementing
/// this interface directly, as it provides pass-through default behavior.
/// </remarks>
public interface IFacadeInterceptor
{
    /// <summary>
    /// Intercepts a synchronous facade method invocation (void or result).
    /// </summary>
    /// <typeparam name="TResult">The return type of the method, or <see cref="object"/> for void methods.</typeparam>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="next">Delegate to invoke the next interceptor or target method.</param>
    /// <returns>The method result, or default for void methods.</returns>
    TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next);

    /// <summary>
    /// Intercepts an asynchronous facade method invocation (Task or Task&lt;T&gt;).
    /// </summary>
    /// <typeparam name="TResult">The return type of the async method, or <see cref="object"/> for Task (non-generic) methods.</typeparam>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="next">Delegate to invoke the next interceptor or target method.</param>
    /// <returns>The method result, or default for Task (non-generic) methods.</returns>
    ValueTask<TResult?> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next);

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
