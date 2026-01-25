namespace Terminus.Interceptors.Abstractions;

/// <summary>
/// Provides rate limiting capabilities for controlling the frequency of method invocations.
/// </summary>
/// <remarks>
/// Implement this interface to integrate with your rate limiting system (e.g., System.Threading.RateLimiting, Redis, custom solution).
/// The service is used by <see cref="RateLimitInterceptor"/> to enforce rate limits on facade method invocations.
/// </remarks>
public interface IRateLimiter
{
    /// <summary>
    /// Attempts to acquire a rate limit permit synchronously.
    /// </summary>
    /// <param name="key">The unique key identifying the rate limit bucket (e.g., method name, user ID).</param>
    /// <param name="maxRequests">The maximum number of requests allowed within the time window.</param>
    /// <param name="window">The time window for the rate limit.</param>
    /// <returns><c>true</c> if a permit was successfully acquired; otherwise, <c>false</c>.</returns>
    bool TryAcquire(string key, int maxRequests, TimeSpan window);

    /// <summary>
    /// Attempts to acquire a rate limit permit asynchronously.
    /// </summary>
    /// <param name="key">The unique key identifying the rate limit bucket (e.g., method name, user ID).</param>
    /// <param name="maxRequests">The maximum number of requests allowed within the time window.</param>
    /// <param name="window">The time window for the rate limit.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, with a result of <c>true</c> if a permit was successfully acquired; otherwise, <c>false</c>.</returns>
    Task<bool> TryAcquireAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken = default);
}
