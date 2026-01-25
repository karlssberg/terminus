using System.Threading.Tasks;

namespace Terminus;

/// <summary>
/// Intercepts asynchronous facade method invocations (Task or Task&lt;T&gt; returning methods).
/// </summary>
/// <remarks>
/// Implement this interface when you only need to intercept asynchronous methods.
/// For full interception including sync and streaming, implement <see cref="IFacadeInterceptor"/>
/// or inherit from <see cref="FacadeInterceptor"/>.
/// </remarks>
public interface IAsyncFacadeInterceptor
{
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
}
