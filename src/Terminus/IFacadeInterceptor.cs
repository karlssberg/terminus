namespace Terminus;

/// <summary>
/// Intercepts all types of facade method invocations for cross-cutting concerns.
/// Interceptors execute in a chain, allowing multiple concerns to be composed.
/// Supports synchronous, asynchronous, and streaming methods, both void and result-returning.
/// </summary>
/// <remarks>
/// <para>
/// This interface combines all five interceptor interfaces:
/// <list type="bullet">
/// <item><see cref="ISyncVoidFacadeInterceptor"/> - For synchronous void methods</item>
/// <item><see cref="ISyncFacadeInterceptor"/> - For synchronous result methods</item>
/// <item><see cref="IAsyncVoidFacadeInterceptor"/> - For asynchronous void methods (Task)</item>
/// <item><see cref="IAsyncFacadeInterceptor"/> - For asynchronous result methods (Task&lt;T&gt;)</item>
/// <item><see cref="IStreamFacadeInterceptor"/> - For streaming methods (IAsyncEnumerable&lt;T&gt;)</item>
/// </list>
/// </para>
/// <para>
/// For interceptors that only need to handle specific method types, implement the individual interfaces.
/// </para>
/// <para>
/// Most users should inherit from <see cref="FacadeInterceptor"/> instead of implementing
/// this interface directly, as it provides pass-through default behavior.
/// </para>
/// <para>
/// All <c>next</c> delegates accept an optional <c>handlers</c> parameter for filtering.
/// Call <c>next()</c> to pass through all handlers, or <c>next(filteredHandlers)</c> to filter.
/// </para>
/// </remarks>
public interface IFacadeInterceptor :
    ISyncVoidFacadeInterceptor,
    ISyncFacadeInterceptor,
    IAsyncVoidFacadeInterceptor,
    IAsyncFacadeInterceptor,
    IStreamFacadeInterceptor;
