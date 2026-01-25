namespace Terminus;

/// <summary>
/// Intercepts all types of facade method invocations for cross-cutting concerns.
/// Interceptors execute in a chain, allowing multiple concerns to be composed.
/// Supports synchronous, asynchronous, and streaming methods.
/// </summary>
/// <remarks>
/// <para>
/// This interface combines <see cref="ISyncFacadeInterceptor"/>, <see cref="IAsyncFacadeInterceptor"/>,
/// and <see cref="IStreamFacadeInterceptor"/> for interceptors that need to handle all method types.
/// </para>
/// <para>
/// For interceptors that only need to handle specific method types, implement the individual interfaces:
/// <list type="bullet">
/// <item><see cref="ISyncFacadeInterceptor"/> - For synchronous methods (void or result)</item>
/// <item><see cref="IAsyncFacadeInterceptor"/> - For asynchronous methods (Task or Task&lt;T&gt;)</item>
/// <item><see cref="IStreamFacadeInterceptor"/> - For streaming methods (IAsyncEnumerable&lt;T&gt;)</item>
/// </list>
/// </para>
/// <para>
/// Most users should inherit from <see cref="FacadeInterceptor"/> instead of implementing
/// this interface directly, as it provides pass-through default behavior.
/// </para>
/// </remarks>
public interface IFacadeInterceptor : ISyncFacadeInterceptor, IAsyncFacadeInterceptor, IStreamFacadeInterceptor;
