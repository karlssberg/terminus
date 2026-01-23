# Interceptors

Interceptors enable cross-cutting concerns like logging, caching, validation, or metrics by wrapping facade method invocations in a configurable pipeline. Each interceptor can inspect, modify, or short-circuit invocations before they reach the target method.

## Overview

The interceptor pattern in Terminus follows the middleware/pipeline model common in ASP.NET Core. When you call a facade method, the invocation passes through each configured interceptor in order before reaching the actual implementation.

**Key Benefits:**
- **Separation of Concerns**: Keep cross-cutting logic separate from business logic
- **Composability**: Combine multiple interceptors for different behaviors
- **Reusability**: Apply the same interceptor across multiple facades
- **Testability**: Interceptors can be mocked or replaced in tests

## Creating an Interceptor

### Using the Base Class

The simplest way to create an interceptor is to extend `FacadeInterceptor`:

```csharp
public class LoggingInterceptor : FacadeInterceptor
{
    private readonly ILogger<LoggingInterceptor> _logger;

    public LoggingInterceptor(ILogger<LoggingInterceptor> logger)
    {
        _logger = logger;
    }

    public override TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next) where TResult : default
    {
        _logger.LogInformation("Calling {Method}", context.Method.Name);
        try
        {
            return next();
        }
        finally
        {
            _logger.LogInformation("Completed {Method}", context.Method.Name);
        }
    }
}
```

The `FacadeInterceptor` base class provides default pass-through implementations for all interceptor methods, so you only need to override the ones you care about.

### Implementing the Interface

For more control, implement `IFacadeInterceptor` directly:

```csharp
public class TimingInterceptor : IFacadeInterceptor
{
    public TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return next();
        }
        finally
        {
            Console.WriteLine($"{context.Method.Name} took {sw.ElapsedMilliseconds}ms");
        }
    }

    public async ValueTask<TResult?> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await next().ConfigureAwait(false);
        }
        finally
        {
            Console.WriteLine($"{context.Method.Name} took {sw.ElapsedMilliseconds}ms");
        }
    }

    public IAsyncEnumerable<TItem> InterceptStream<TItem>(
        FacadeInvocationContext context,
        FacadeStreamInvocationDelegate<TItem> next)
    {
        // Pass through for streams
        return next();
    }
}
```

## Interceptor Methods

Each interceptor can handle three types of invocations:

| Method | Return Type | Description |
| :--- | :--- | :--- |
| `Intercept<TResult>` | `TResult?` | Handles sync methods (void, T) |
| `InterceptAsync<TResult>` | `ValueTask<TResult?>` | Handles async methods (Task, Task<T>, ValueTask, ValueTask<T>) |
| `InterceptStream<TItem>` | `IAsyncEnumerable<TItem>` | Handles streaming methods (IAsyncEnumerable<T>) |

## Registering Interceptors

### On the Facade

Specify interceptors using the `Interceptors` property:

```csharp
[FacadeOf<HandlerAttribute>(Interceptors = [typeof(LoggingInterceptor), typeof(CachingInterceptor)])]
public partial interface IAppFacade { }
```

### In Dependency Injection

Interceptors are resolved from the service provider, so register them:

```csharp
services.AddSingleton<LoggingInterceptor>();
services.AddSingleton<CachingInterceptor>();
```

Use appropriate lifetimes:
- **Singleton**: For stateless interceptors or those with thread-safe state
- **Scoped**: For interceptors that need per-request state
- **Transient**: For interceptors with per-invocation state (rarely needed)

## Interceptor Pipeline

Interceptors execute in the order they are declared:

```
Request → LoggingInterceptor → CachingInterceptor → ValidationInterceptor → Target Method
                                                                                  ↓
Response ← LoggingInterceptor ← CachingInterceptor ← ValidationInterceptor ← Target Method
```

Each interceptor:
1. Receives the invocation context
2. Can perform pre-invocation logic
3. Calls `next()` to continue the pipeline
4. Can perform post-invocation logic
5. Returns the result (or a modified result)

### Short-Circuiting

An interceptor can skip downstream interceptors and the target method by not calling `next()`:

```csharp
public class CachingInterceptor : FacadeInterceptor
{
    private readonly IMemoryCache _cache;

    public override TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next) where TResult : default
    {
        var cacheKey = BuildCacheKey(context);

        // Short-circuit if cached
        if (_cache.TryGetValue(cacheKey, out TResult? cached))
            return cached;  // Does NOT call next()

        // Continue pipeline
        var result = next();
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }
}
```

## FacadeInvocationContext

The context provides rich metadata about the invocation:

| Property | Type | Description |
| :--- | :--- | :--- |
| `ServiceProvider` | `IServiceProvider` | For resolving additional dependencies |
| `Method` | `MethodInfo` | Reflection info for the facade method |
| `Arguments` | `object?[]` | The arguments passed to the method |
| `TargetType` | `Type` | The type containing the implementation |
| `MethodAttribute` | `Attribute` | The facade attribute on the implementation method |
| `Properties` | `IDictionary<string, object?>` | Mutable dictionary for passing data between interceptors |
| `ReturnTypeKind` | `ReturnTypeKind` | The return type category (Void, Result, Task, TaskWithResult, AsyncEnumerable) |

### Using Properties for Inter-Interceptor Communication

```csharp
public class CorrelationInterceptor : FacadeInterceptor
{
    public override TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next) where TResult : default
    {
        // Set correlation ID for downstream interceptors
        context.Properties["CorrelationId"] = Guid.NewGuid().ToString();
        return next();
    }
}

public class LoggingInterceptor : FacadeInterceptor
{
    public override TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next) where TResult : default
    {
        // Read correlation ID from upstream interceptor
        var correlationId = context.Properties.TryGetValue("CorrelationId", out var id)
            ? id?.ToString()
            : "unknown";

        _logger.LogInformation("[{CorrelationId}] Calling {Method}",
            correlationId, context.Method.Name);

        return next();
    }
}
```

## Common Interceptor Patterns

### Logging Interceptor

```csharp
public class LoggingInterceptor : FacadeInterceptor
{
    private readonly ILogger<LoggingInterceptor> _logger;

    public LoggingInterceptor(ILogger<LoggingInterceptor> logger) => _logger = logger;

    public override TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next) where TResult : default
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Entering {Method} with {ArgCount} args",
            context.Method.Name, context.Arguments.Length);

        try
        {
            var result = next();
            _logger.LogDebug("Exiting {Method} after {Elapsed}ms",
                context.Method.Name, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in {Method} after {Elapsed}ms",
                context.Method.Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public override async ValueTask<TResult?> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next) where TResult : default
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Entering async {Method}", context.Method.Name);

        try
        {
            var result = await next().ConfigureAwait(false);
            _logger.LogDebug("Exiting async {Method} after {Elapsed}ms",
                context.Method.Name, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in async {Method} after {Elapsed}ms",
                context.Method.Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### Caching Interceptor

```csharp
public class CachingInterceptor : FacadeInterceptor
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _duration = TimeSpan.FromMinutes(5);

    public CachingInterceptor(IMemoryCache cache) => _cache = cache;

    public override TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next) where TResult : default
    {
        // Don't cache void methods
        if (context.ReturnTypeKind == ReturnTypeKind.Void)
            return next();

        var key = BuildCacheKey(context);

        if (_cache.TryGetValue(key, out TResult? cached))
            return cached;

        var result = next();
        _cache.Set(key, result, _duration);
        return result;
    }

    public override async ValueTask<TResult?> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next) where TResult : default
    {
        var key = BuildCacheKey(context);

        if (_cache.TryGetValue(key, out TResult? cached))
            return cached;

        var result = await next().ConfigureAwait(false);
        _cache.Set(key, result, _duration);
        return result;
    }

    private static string BuildCacheKey(FacadeInvocationContext context)
    {
        var args = string.Join(":", context.Arguments.Select(a => a?.ToString() ?? "null"));
        return $"{context.Method.DeclaringType?.Name}.{context.Method.Name}:{args}";
    }
}
```

### Validation Interceptor

```csharp
public class ValidationInterceptor : FacadeInterceptor
{
    public override TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next) where TResult : default
    {
        ValidateArguments(context);
        return next();
    }

    public override async ValueTask<TResult?> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next) where TResult : default
    {
        ValidateArguments(context);
        return await next().ConfigureAwait(false);
    }

    private static void ValidateArguments(FacadeInvocationContext context)
    {
        var parameters = context.Method.GetParameters();

        for (int i = 0; i < context.Arguments.Length; i++)
        {
            var param = parameters[i];
            var arg = context.Arguments[i];

            if (arg is null && !IsNullable(param.ParameterType))
            {
                throw new ArgumentNullException(param.Name,
                    $"Parameter '{param.Name}' cannot be null.");
            }

            // Add more validation rules as needed
            if (arg is IValidatableObject validatable)
            {
                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(validatable, new ValidationContext(validatable), results, true))
                {
                    throw new ValidationException(string.Join("; ", results.Select(r => r.ErrorMessage)));
                }
            }
        }
    }

    private static bool IsNullable(Type type) =>
        !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
}
```

### Retry Interceptor

```csharp
public class RetryInterceptor : FacadeInterceptor
{
    private readonly int _maxRetries = 3;
    private readonly TimeSpan _delay = TimeSpan.FromMilliseconds(100);

    public override TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next) where TResult : default
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return next();
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < _maxRetries)
            {
                lastException = ex;
                Thread.Sleep(_delay * attempt);
            }
        }

        throw lastException!;
    }

    public override async ValueTask<TResult?> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next) where TResult : default
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await next().ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < _maxRetries)
            {
                lastException = ex;
                await Task.Delay(_delay * attempt).ConfigureAwait(false);
            }
        }

        throw lastException!;
    }

    private static bool IsTransient(Exception ex) =>
        ex is HttpRequestException or TimeoutException or OperationCanceledException;
}
```

### Stream Counting Interceptor

```csharp
public class StreamCountingInterceptor : FacadeInterceptor
{
    private readonly ILogger<StreamCountingInterceptor> _logger;

    public StreamCountingInterceptor(ILogger<StreamCountingInterceptor> logger)
    {
        _logger = logger;
    }

    public override IAsyncEnumerable<TItem> InterceptStream<TItem>(
        FacadeInvocationContext context,
        FacadeStreamInvocationDelegate<TItem> next)
    {
        return WrapStream(context, next());
    }

    private async IAsyncEnumerable<TItem> WrapStream<TItem>(
        FacadeInvocationContext context,
        IAsyncEnumerable<TItem> source)
    {
        var count = 0;
        var sw = Stopwatch.StartNew();

        await foreach (var item in source)
        {
            count++;
            yield return item;
        }

        _logger.LogInformation("Stream {Method} yielded {Count} items in {Elapsed}ms",
            context.Method.Name, count, sw.ElapsedMilliseconds);
    }
}
```

## Combining Interceptors

Order interceptors from outer (first) to inner (last) based on their responsibilities:

```csharp
[FacadeOf<HandlerAttribute>(Interceptors = [
    typeof(ExceptionHandlingInterceptor),  // Outermost: catches all exceptions
    typeof(CorrelationInterceptor),        // Adds correlation ID
    typeof(LoggingInterceptor),            // Logs with correlation ID
    typeof(ValidationInterceptor),         // Validates before execution
    typeof(CachingInterceptor),            // Returns cached results
    typeof(RetryInterceptor)               // Innermost: retries on failure
])]
public partial interface IRobustFacade { }
```

## Best Practices

### 1. Keep Interceptors Focused

Each interceptor should have a single responsibility:

```csharp
// Good: Single responsibility
public class LoggingInterceptor : FacadeInterceptor { /* logging only */ }
public class CachingInterceptor : FacadeInterceptor { /* caching only */ }
public class ValidationInterceptor : FacadeInterceptor { /* validation only */ }

// Bad: Multiple responsibilities
public class KitchenSinkInterceptor : FacadeInterceptor
{
    // Does logging AND caching AND validation - hard to test and maintain
}
```

### 2. Always Call ConfigureAwait(false)

For async interceptors, use `ConfigureAwait(false)`:

```csharp
public override async ValueTask<TResult?> InterceptAsync<TResult>(
    FacadeInvocationContext context,
    FacadeAsyncInvocationDelegate<TResult> next) where TResult : default
{
    // Always use ConfigureAwait(false) for library code
    return await next().ConfigureAwait(false);
}
```

### 3. Handle Exceptions Appropriately

Decide whether to catch, log, transform, or rethrow exceptions:

```csharp
public override TResult? Intercept<TResult>(
    FacadeInvocationContext context,
    FacadeInvocationDelegate<TResult> next) where TResult : default
{
    try
    {
        return next();
    }
    catch (Exception ex)
    {
        // Log but rethrow - don't swallow exceptions silently
        _logger.LogError(ex, "Error in {Method}", context.Method.Name);
        throw;
    }
}
```

### 4. Use Properties for State

Use `context.Properties` instead of instance fields for request-scoped state:

```csharp
// Good: Uses context properties
context.Properties["StartTime"] = Stopwatch.StartNew();

// Bad: Instance field (shared across concurrent calls)
_startTime = Stopwatch.StartNew();
```

### 5. Consider Performance

Interceptors run on every invocation. Keep them lightweight:

- Cache reflection results
- Avoid unnecessary allocations
- Use span-based APIs where possible
- Consider short-circuiting for inapplicable cases

## Limitations

- **Method aggregation**: Interceptors do not apply to aggregated method groups with `IncludeAttributeMetadata = true`
- **Static methods**: Interceptors apply to both static and instance methods
- **Scoped facades**: Interceptors are resolved from the root service provider, not the scope

## See Also

- [Advanced Scenarios](../guides/advanced-scenarios.md#interceptors) - Practical interceptor examples
- [Service Resolution](service-resolution.md) - Understanding how services are resolved
- [Facades](facades.md) - Core facade concepts
