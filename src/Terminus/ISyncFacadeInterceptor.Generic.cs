using System;

namespace Terminus;

/// <summary>
/// Intercepts synchronous result-returning facade method invocations with strongly-typed attribute context.
/// </summary>
/// <typeparam name="TAttribute">The attribute type that marks facade methods.</typeparam>
/// <remarks>
/// <para>
/// Implement this interface when you need to intercept result-returning methods with access to the
/// strongly-typed facade method attribute. This allows compile-time safe access to attribute
/// properties and metadata.
/// </para>
/// <para>
/// The <paramref name="next"/> delegate accepts an optional handlers parameter for filtering.
/// Call <c>next()</c> to pass through all handlers, or <c>next(filteredHandlers)</c> to filter.
/// </para>
/// </remarks>
public interface ISyncFacadeInterceptor<TAttribute> where TAttribute : Attribute
{
    /// <summary>
    /// Intercepts a synchronous result-returning facade method invocation.
    /// </summary>
    /// <typeparam name="TResult">The return type of the method.</typeparam>
    /// <param name="context">The strongly-typed invocation context containing method metadata and the facade method attribute.</param>
    /// <param name="next">
    /// Delegate to invoke the next interceptor or target method.
    /// Call with no arguments for pass-through, or with filtered handlers list.
    /// </param>
    /// <returns>The method result.</returns>
    TResult Intercept<TResult>(
        FacadeInvocationContext<TAttribute> context,
        FacadeInvocationDelegate<TResult> next);
}
