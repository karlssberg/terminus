using System;
using System.Threading.Tasks;

namespace Terminus;

/// <summary>
/// Intercepts asynchronous void facade method invocations with strongly-typed attribute context.
/// </summary>
/// <typeparam name="TAttribute">The attribute type that marks facade methods.</typeparam>
/// <remarks>
/// <para>
/// Implement this interface when you need to intercept async void methods (Task-returning) with access to the
/// strongly-typed facade method attribute. This allows compile-time safe access to attribute
/// properties and metadata.
/// </para>
/// <para>
/// The <paramref name="next"/> delegate accepts an optional handlers parameter for filtering.
/// Call <c>next()</c> to pass through all handlers, or <c>next(filteredHandlers)</c> to filter.
/// </para>
/// </remarks>
public interface IAsyncVoidFacadeInterceptor<TAttribute> where TAttribute : Attribute
{
    /// <summary>
    /// Intercepts an asynchronous void facade method invocation.
    /// </summary>
    /// <param name="context">The strongly-typed invocation context containing method metadata and the facade method attribute.</param>
    /// <param name="next">
    /// Delegate to invoke the next interceptor or target method.
    /// Call with no arguments for pass-through, or with filtered handlers list.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask InterceptAsync(
        FacadeInvocationContext<TAttribute> context,
        FacadeAsyncVoidInvocationDelegate next);
}
