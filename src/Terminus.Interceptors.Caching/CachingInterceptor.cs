using Microsoft.Extensions.Caching.Memory;

namespace Terminus.Interceptors.Caching;

/// <summary>
/// Intercepts facade method invocations to cache results for query methods (non-void return types) using in-memory caching.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor only caches methods that return values (skips void and Task methods).
/// It uses <see cref="FacadeInvocationContext.ReturnTypeKind"/> to determine cacheability.
/// </para>
/// <para>
/// Results are stored directly in <see cref="IMemoryCache"/>.
/// For distributed caching, use <see cref="DistributedCachingInterceptor"/> instead.
/// </para>
/// <para>
/// Cache keys are generated from the method name and arguments.
/// Cache hits/misses are tracked in <see cref="FacadeInvocationContext.Properties"/> under the "CacheHit" key.
/// </para>
/// </remarks>
public class CachingInterceptor : FacadeInterceptor
{
    private readonly IMemoryCache _memoryCache;
    private readonly TimeSpan _defaultExpiration;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingInterceptor"/> class.
    /// </summary>
    /// <param name="memoryCache">The in-memory cache.</param>
    /// <param name="defaultExpiration">The default cache expiration time. Defaults to 5 minutes if not specified.</param>
    public CachingInterceptor(IMemoryCache memoryCache, TimeSpan? defaultExpiration = null)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Intercepts synchronous facade method invocations (void or result methods).
    /// Caches results for query methods (non-void return types).
    /// </summary>
    public override TResult Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next) where TResult : default
    {
        // Only cache query methods (non-void return types)
        if (context.ReturnTypeKind == ReturnTypeKind.Void)
        {
            return next();
        }

        var cacheKey = GenerateCacheKey(context);

        if (_memoryCache.TryGetValue(cacheKey, out var cachedResult) && cachedResult is TResult value)
        {
            context.Properties["CacheHit"] = true;
            return value;
        }

        var result = next();
        _memoryCache.Set(cacheKey, result, _defaultExpiration);
        context.Properties["CacheHit"] = false;
        return result;
    }

    /// <summary>
    /// Intercepts asynchronous facade method invocations (Task or Task&lt;T&gt; methods).
    /// Caches results for async query methods (Task&lt;T&gt; return types).
    /// </summary>
    public override async ValueTask<TResult> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next) where TResult : default
    {
        // Only cache async query methods (Task<T>, not Task)
        if (context.ReturnTypeKind != ReturnTypeKind.TaskWithResult)
        {
            return await next();
        }

        var cacheKey = GenerateCacheKey(context);

        if (_memoryCache.TryGetValue(cacheKey, out var cachedResult) && cachedResult is TResult value)
        {
            context.Properties["CacheHit"] = true;
            return value;
        }

        var result = await next();
        _memoryCache.Set(cacheKey, result, _defaultExpiration);
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
        var args = context.Arguments;
        var argString = args.Length == 0
            ? string.Empty
            : string.Join(",", args.Select(a => a?.ToString() ?? "null"));

        return $"{context.Method.DeclaringType?.FullName}.{context.Method.Name}({argString})";
    }
}
