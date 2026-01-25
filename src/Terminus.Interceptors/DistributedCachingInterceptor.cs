using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Terminus.Interceptors;

/// <summary>
/// Intercepts facade method invocations to cache results for async query methods using distributed caching.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor only caches async methods that return values (Task&lt;T&gt; or ValueTask&lt;T&gt;).
/// It skips void methods, Task methods, and synchronous methods.
/// </para>
/// <para>
/// Results are serialized to JSON and stored in <see cref="IDistributedCache"/>.
/// </para>
/// <para>
/// Cache keys are generated from the method name and arguments.
/// Cache hits/misses are tracked in <see cref="FacadeInvocationContext.Properties"/> under the "CacheHit" key.
/// </para>
/// </remarks>
public class DistributedCachingInterceptor : FacadeInterceptor
{
    private readonly IDistributedCache _distributedCache;
    private readonly TimeSpan _defaultExpiration;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedCachingInterceptor"/> class.
    /// </summary>
    /// <param name="distributedCache">The distributed cache.</param>
    /// <param name="defaultExpiration">The default cache expiration time. Defaults to 5 minutes if not specified.</param>
    public DistributedCachingInterceptor(IDistributedCache distributedCache, TimeSpan? defaultExpiration = null)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(5);
        _jsonOptions = new JsonSerializerOptions { WriteIndented = false };
    }

    /// <summary>
    /// Intercepts synchronous facade method invocations.
    /// Distributed caching does not support synchronous operations effectively, so this passes through to the next handler.
    /// </summary>
    public override TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next) where TResult : default
    {
        // Distributed cache doesn't support sync caching efficiently
        // Pass through to next handler
        return next();
    }

    /// <summary>
    /// Intercepts asynchronous facade method invocations (Task or Task&lt;T&gt; methods).
    /// Caches results for async query methods (Task&lt;T&gt; return types).
    /// </summary>
    public override async ValueTask<TResult?> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next) where TResult : default
    {
        // Only cache async query methods (Task<T>, not Task or void)
        if (context.ReturnTypeKind != ReturnTypeKind.TaskWithResult)
        {
            return await next();
        }

        var cacheKey = GenerateCacheKey(context);

        // Try to get from cache
        var cachedBytes = await _distributedCache.GetAsync(cacheKey);
        if (cachedBytes != null)
        {
            context.Properties["CacheHit"] = true;
            return JsonSerializer.Deserialize<TResult>(cachedBytes, _jsonOptions);
        }

        // Cache miss - execute and cache result
        var result = await next();
        var serialized = JsonSerializer.SerializeToUtf8Bytes(result, _jsonOptions);

        await _distributedCache.SetAsync(
            cacheKey,
            serialized,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _defaultExpiration });

        context.Properties["CacheHit"] = false;
        return result;
    }

    // Streaming methods are not cached (would require buffering entire stream)
    // Base class provides pass-through behavior

    /// <summary>
    /// Generates a cache key from the method name and arguments.
    /// </summary>
    private static string GenerateCacheKey(FacadeInvocationContext context)
    {
        var args = context.Arguments ?? [];
        var argString = args.Length == 0
            ? string.Empty
            : string.Join(",", args.Select(a => a?.ToString() ?? "null"));

        return $"{context.Method.DeclaringType?.FullName}.{context.Method.Name}({argString})";
    }
}
