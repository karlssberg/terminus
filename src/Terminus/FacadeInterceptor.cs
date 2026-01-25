using System.Collections.Generic;
using System.Threading.Tasks;

namespace Terminus;

/// <summary>
/// Base class for facade interceptors with pass-through default behavior.
/// Override only the methods you need to intercept.
/// </summary>
/// <remarks>
/// <para>
/// This class provides sensible defaults by calling <c>next()</c> for all methods.
/// Override specific methods to add interception logic for sync, async, or streaming methods.
/// </para>
/// <para>
/// All <c>next</c> delegates accept an optional <c>handlers</c> parameter for filtering.
/// Call <c>next()</c> to pass through all handlers, or <c>next(filteredHandlers)</c> to filter.
/// </para>
/// </remarks>
public abstract class FacadeInterceptor : IFacadeInterceptor
{
    /// <summary>
    /// Intercepts a synchronous void facade method invocation.
    /// Default implementation calls the next interceptor or target method.
    /// </summary>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="next">Delegate to invoke the next interceptor or target method.</param>
    public virtual void Intercept(
        FacadeInvocationContext context,
        FacadeVoidInvocationDelegate next)
    {
        next();
    }

    /// <summary>
    /// Intercepts a synchronous result-returning facade method invocation.
    /// Default implementation calls the next interceptor or target method.
    /// </summary>
    /// <typeparam name="TResult">The return type of the method.</typeparam>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="next">Delegate to invoke the next interceptor or target method.</param>
    /// <returns>The method result.</returns>
    public virtual TResult Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next)
    {
        return next();
    }

    /// <summary>
    /// Intercepts an asynchronous void facade method invocation (Task or ValueTask).
    /// Default implementation calls the next interceptor or target method.
    /// </summary>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="next">Delegate to invoke the next interceptor or target method.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual Task InterceptAsync(
        FacadeInvocationContext context,
        FacadeAsyncVoidInvocationDelegate next)
    {
        return next();
    }

    /// <summary>
    /// Intercepts an asynchronous result-returning facade method invocation (Task&lt;T&gt; or ValueTask&lt;T&gt;).
    /// Default implementation calls the next interceptor or target method.
    /// </summary>
    /// <typeparam name="TResult">The return type of the async method.</typeparam>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="next">Delegate to invoke the next interceptor or target method.</param>
    /// <returns>The method result.</returns>
    public virtual ValueTask<TResult> InterceptAsync<TResult>(
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
