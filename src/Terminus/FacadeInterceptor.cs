using System.Collections.Generic;
using System.Threading.Tasks;

namespace Terminus;

/// <summary>
/// Base class for facade interceptors with pass-through default behavior.
/// Override only the methods you need to intercept.
/// </summary>
/// <remarks>
/// This class provides sensible defaults by calling <c>next()</c> for all methods.
/// Override specific methods to add interception logic for sync, async, or streaming methods.
/// </remarks>
public abstract class FacadeInterceptor : IFacadeInterceptor
{
    /// <summary>
    /// Intercepts a synchronous facade method invocation (void or result).
    /// Default implementation calls the next interceptor or target method.
    /// </summary>
    /// <typeparam name="TResult">The return type of the method, or <see cref="object"/> for void methods.</typeparam>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="next">Delegate to invoke the next interceptor or target method.</param>
    /// <returns>The method result, or default for void methods.</returns>
    public virtual TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next)
    {
        return next();
    }

    /// <summary>
    /// Intercepts an asynchronous facade method invocation (Task or Task&lt;T&gt;).
    /// Default implementation calls the next interceptor or target method.
    /// </summary>
    /// <typeparam name="TResult">The return type of the async method, or <see cref="object"/> for Task (non-generic) methods.</typeparam>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="next">Delegate to invoke the next interceptor or target method.</param>
    /// <returns>The method result, or default for Task (non-generic) methods.</returns>
    public virtual ValueTask<TResult?> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next)
    {
        return next();
    }

    /// <summary>
    /// Intercepts a streaming facade method invocation (IAsyncEnumerable&lt;T&gt;).
    /// Default implementation passes through all items from the next interceptor or target method.
    /// </summary>
    /// <typeparam name="TItem">The type of items in the stream.</typeparam>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="next">Delegate to invoke the next interceptor or target method stream.</param>
    /// <returns>An async enumerable of items.</returns>
    public virtual async IAsyncEnumerable<TItem> InterceptStream<TItem>(
        FacadeInvocationContext context,
        FacadeStreamInvocationDelegate<TItem> next)
    {
        await foreach (var item in next())
        {
            yield return item;
        }
    }
}
