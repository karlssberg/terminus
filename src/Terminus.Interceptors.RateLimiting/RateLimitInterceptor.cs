namespace Terminus.Interceptors.RateLimiting;

/// <summary>
/// Intercepts facade method invocations to enforce rate limits.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor uses <see cref="IRateLimiter"/> to check and enforce rate limits.
/// The rate limit configuration is extracted from a custom attribute on the method (e.g., <c>RateLimitAttribute</c>).
/// </para>
/// <para>
/// If the rate limit is exceeded, a <see cref="RateLimitExceededException"/> is thrown.
/// The rate limit key can be specified in the attribute or defaults to the method name.
/// </para>
/// </remarks>
public class RateLimitInterceptor(IRateLimiter rateLimiter) : FacadeInterceptor
{
    private readonly IRateLimiter _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));

    /// <summary>
    /// Intercepts synchronous facade method invocations (void or result methods).
    /// </summary>
    public override TResult Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next) where TResult : default
    {
        var (key, maxRequests, window) = ExtractRateLimitConfig(context);
        if (key != null && !_rateLimiter.TryAcquire(key, maxRequests, window))
        {
            throw new RateLimitExceededException(key);
        }

        return next();
    }

    /// <summary>
    /// Intercepts asynchronous facade method invocations (Task or Task&lt;T&gt; methods).
    /// </summary>
    public override async ValueTask<TResult> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next) where TResult : default
    {
        var (key, maxRequests, window) = ExtractRateLimitConfig(context);
        if (key != null && !await _rateLimiter.TryAcquireAsync(key, maxRequests, window))
        {
            throw new RateLimitExceededException(key);
        }

        return await next();
    }

    /// <summary>
    /// Intercepts streaming facade method invocations (IAsyncEnumerable&lt;T&gt; methods).
    /// </summary>
    public override async IAsyncEnumerable<TItem> InterceptStream<TItem>(
        FacadeInvocationContext context,
        FacadeStreamInvocationDelegate<TItem> next)
    {
        var (key, maxRequests, window) = ExtractRateLimitConfig(context);
        if (key != null && !await _rateLimiter.TryAcquireAsync(key, maxRequests, window))
        {
            throw new RateLimitExceededException(key);
        }

        await foreach (var item in next())
        {
            yield return item;
        }
    }

    /// <summary>
    /// Extracts rate limit configuration from the method attribute.
    /// </summary>
    /// <remarks>
    /// This method looks for properties: Key, MaxRequests, Window/WindowSeconds on the attribute.
    /// If MaxRequests is not found or is zero, rate limiting is skipped (returns null key).
    /// The key defaults to the method name if not specified in the attribute.
    /// </remarks>
    private static (string? Key, int MaxRequests, TimeSpan Window) ExtractRateLimitConfig(FacadeInvocationContext context)
    {
        var attribute = context.MethodAttribute;
        if (attribute == null)
        {
            return (null, 0, TimeSpan.Zero);
        }

        var type = attribute.GetType();

        // Try to extract max requests
        var maxRequestsProp = type.GetProperty("MaxRequests");
        if (maxRequestsProp?.PropertyType != typeof(int))
        {
            return (null, 0, TimeSpan.Zero);
        }

        var maxRequests = (int?)maxRequestsProp.GetValue(attribute) ?? 0;
        if (maxRequests <= 0)
        {
            return (null, 0, TimeSpan.Zero);
        }

        // Try to extract window
        var windowProp = type.GetProperty("Window");
        TimeSpan window;

        if (windowProp?.PropertyType == typeof(TimeSpan))
        {
            window = (TimeSpan?)windowProp.GetValue(attribute) ?? TimeSpan.FromMinutes(1);
        }
        else
        {
            // Try WindowSeconds as fallback
            var windowSecondsProp = type.GetProperty("WindowSeconds");
            if (windowSecondsProp?.PropertyType == typeof(int))
            {
                var windowSeconds = (int?)windowSecondsProp.GetValue(attribute) ?? 60;
                window = TimeSpan.FromSeconds(windowSeconds);
            }
            else
            {
                window = TimeSpan.FromMinutes(1);
            }
        }

        // Try to extract key (defaults to method name)
        var keyProp = type.GetProperty("Key");
        var key = (keyProp?.PropertyType == typeof(string)
            ? keyProp.GetValue(attribute) as string
            : null) ?? context.Method.Name;

        return (key, maxRequests, window);
    }
}
