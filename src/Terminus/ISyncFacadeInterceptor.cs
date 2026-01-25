namespace Terminus;

/// <summary>
/// Intercepts synchronous facade method invocations (void or result-returning methods).
/// </summary>
/// <remarks>
/// Implement this interface when you only need to intercept synchronous methods.
/// For full interception including async and streaming, implement <see cref="IFacadeInterceptor"/>
/// or inherit from <see cref="FacadeInterceptor"/>.
/// </remarks>
public interface ISyncFacadeInterceptor
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
}
