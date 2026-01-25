using System.Collections.Generic;

namespace Terminus;

/// <summary>
/// Interceptor that can filter handlers in aggregated facade methods.
/// </summary>
/// <remarks>
/// <para>
/// This interface extends <see cref="IFacadeInterceptor"/> to provide handler filtering capabilities
/// for aggregated methods (multiple handlers with the same signature).
/// </para>
/// <para>
/// Interceptors implementing this interface can control which handlers execute by returning a filtered
/// subset of the handlers passed to <see cref="FilterHandlers"/>.
/// </para>
/// <para>
/// For non-aggregated methods (single handler), filtering still applies but typically passes through
/// the single handler unchanged or filters it out entirely.
/// </para>
/// </remarks>
public interface IAggregatableInterceptor : IFacadeInterceptor
{
    /// <summary>
    /// Filters handlers before execution in a facade method invocation.
    /// </summary>
    /// <param name="context">The invocation context containing method metadata and arguments.</param>
    /// <param name="handlers">All handlers that would be invoked for this method.</param>
    /// <returns>
    /// The handlers that should execute. Return an empty collection to skip all handlers.
    /// The order of returned handlers determines execution order.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is called before handler execution, allowing the interceptor to:
    /// - Filter handlers based on attribute metadata (e.g., feature flags, permissions)
    /// - Reorder handlers based on priority or other criteria
    /// - Skip all handlers by returning an empty collection
    /// </para>
    /// <para>
    /// For aggregated methods, <paramref name="handlers"/> contains multiple descriptors.
    /// For non-aggregated methods, <paramref name="handlers"/> contains a single descriptor.
    /// </para>
    /// <para>
    /// When multiple <see cref="IAggregatableInterceptor"/> instances are configured,
    /// they execute in order, each receiving the filtered result from the previous interceptor.
    /// </para>
    /// </remarks>
    IEnumerable<FacadeHandlerDescriptor> FilterHandlers(
        FacadeInvocationContext context,
        IReadOnlyList<FacadeHandlerDescriptor> handlers);
}
