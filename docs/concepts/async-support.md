# Async Support

Terminus provides comprehensive support for asynchronous programming patterns, including `Task`, `ValueTask`, and `IAsyncEnumerable<T>`. All async operations are generated with proper `await` logic and `ConfigureAwait(false)` for optimal performance.

## Supported Return Types

| Return Type | Category | Await? | ConfigureAwait? | Notes |
|-------------|----------|--------|-----------------|-------|
| `void` | Synchronous | No | No | Standard void method |
| `T` | Synchronous | No | No | Returns a value synchronously |
| `Task` | Async Command | Yes | Yes | Async operation with no result |
| `Task<T>` | Async Query | Yes | Yes | Async operation with result |
| `ValueTask` | Async Command | Yes | Yes | Allocation-efficient async command |
| `ValueTask<T>` | Async Query | Yes | Yes | Allocation-efficient async query |
| `IAsyncEnumerable<T>` | Async Stream | Yes (scoped) | N/A | Async streaming results |

## Task and Task\<T\>

### Basic Async Command (Task)

Methods returning `Task` represent asynchronous operations with no result:

```csharp
public class NotificationService
{
    [Handler]
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await _emailClient.SendAsync(to, subject, body);
    }
}
```

**Generated facade method:**
```csharp
async Task IAppFacade.SendEmailAsync(string to, string subject, string body)
{
    await _serviceProvider.GetRequiredService<NotificationService>()
        .SendEmailAsync(to, subject, body)
        .ConfigureAwait(false);
}
```

### Basic Async Query (Task\<T\>)

Methods returning `Task<T>` represent asynchronous operations with a result:

```csharp
public class UserService
{
    [Handler]
    public async Task<User> GetUserAsync(int id)
    {
        return await _db.Users.FindAsync(id);
    }
}
```

**Generated facade method:**
```csharp
async Task<User> IAppFacade.GetUserAsync(int id)
{
    return await _serviceProvider.GetRequiredService<UserService>()
        .GetUserAsync(id)
        .ConfigureAwait(false);
}
```

### Why ConfigureAwait(false)?

Terminus automatically adds `.ConfigureAwait(false)` to all awaited calls:

```csharp
await method().ConfigureAwait(false);
```

**Benefits:**
- Avoids capturing and resuming on the original synchronization context
- Better performance in library code
- Prevents potential deadlocks
- Standard best practice for non-UI code

**When it matters:**
- ASP.NET Core: No difference (no sync context)
- WPF/WinForms: Would matter, but facades are typically backend code
- Libraries: Always recommended

## ValueTask and ValueTask\<T\>

`ValueTask` and `ValueTask<T>` are allocation-efficient alternatives to `Task` and `Task<T>`, useful for high-performance scenarios.

### ValueTask Support

```csharp
public class CacheService
{
    [Handler]
    public async ValueTask InvalidateCacheAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}
```

**Generated facade method:**
```csharp
async ValueTask IAppFacade.InvalidateCacheAsync(string key)
{
    await _serviceProvider.GetRequiredService<CacheService>()
        .InvalidateCacheAsync(key)
        .ConfigureAwait(false);
}
```

### ValueTask\<T\> Support

```csharp
public class CacheService
{
    [Handler]
    public async ValueTask<string?> GetCachedAsync(string key)
    {
        return await _cache.GetAsync(key);
    }
}
```

**Generated facade method:**
```csharp
async ValueTask<string?> IAppFacade.GetCachedAsync(string key)
{
    return await _serviceProvider.GetRequiredService<CacheService>()
        .GetCachedAsync(key)
        .ConfigureAwait(false);
}
```

### When to Use ValueTask

✅ **Use `ValueTask<T>` when:**
- Result is often available synchronously (e.g., cache hit)
- High-frequency operations where allocation matters
- Performance is critical

❌ **Use `Task<T>` when:**
- Result is always asynchronous
- Method is rarely called
- Simplicity is preferred over micro-optimizations

## Async Streams (IAsyncEnumerable\<T\>)

Terminus supports `IAsyncEnumerable<T>` for streaming results asynchronously.

### Non-Scoped Facades

For non-scoped facades, the async enumerable is returned directly:

```csharp
[FacadeOf(typeof(HandlerAttribute))]  // Scoped = false (default)
public partial interface IAppFacade { }

public class DataService
{
    [Handler]
    public async IAsyncEnumerable<Item> GetItemsAsync()
    {
        await foreach (var item in _repository.StreamItemsAsync())
        {
            yield return item;
        }
    }
}
```

**Generated facade method:**
```csharp
IAsyncEnumerable<Item> IAppFacade.GetItemsAsync()
{
    return _serviceProvider.GetRequiredService<DataService>()
        .GetItemsAsync();
}
```

### Scoped Facades

For scoped facades, Terminus generates a proxy iterator to ensure the scope lifetime:

```csharp
[FacadeOf(typeof(HandlerAttribute), Scoped = true)]
public partial interface IAppFacade { }

public class DataService
{
    private readonly DbContext _db;

    [Handler]
    public async IAsyncEnumerable<Item> GetItemsAsync()
    {
        await foreach (var item in _db.Items.AsAsyncEnumerable())
        {
            yield return item;
        }
    }
}
```

**Generated facade method:**
```csharp
async IAsyncEnumerable<Item> IAppFacade.GetItemsAsync()
{
    await foreach (var item in _asyncScope.Value.ServiceProvider
        .GetRequiredService<DataService>()
        .GetItemsAsync())
    {
        yield return item;
    }
}
```

**Why the difference?**
- Scoped facades need to keep the scope alive while enumerating
- The proxy iterator ensures the scope isn't disposed until enumeration completes
- Non-scoped facades can return directly (service resolved per enumeration)

### Usage

```csharp
await using var facade = provider.GetRequiredService<IAppFacade>();

await foreach (var item in facade.GetItemsAsync())
{
    Console.WriteLine(item.Name);
}
// Scope disposed here (for scoped facades)
```

## CancellationToken Support

### Standard Pattern

Pass `CancellationToken` as a parameter for cancellable async operations:

```csharp
public class DataService
{
    [Handler]
    public async Task<List<Item>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        return await _repository.SearchAsync(query, cancellationToken);
    }
}
```

**Generated facade method:**
```csharp
async Task<List<Item>> IAppFacade.SearchAsync(
    string query,
    CancellationToken cancellationToken = default)
{
    return await _serviceProvider.GetRequiredService<DataService>()
        .SearchAsync(query, cancellationToken)
        .ConfigureAwait(false);
}
```

### Special Case: Static Methods with Only CancellationToken

For static methods with a single `CancellationToken` parameter, Terminus generates a cancellation check:

```csharp
public static class Utilities
{
    [Handler]
    public static void CheckCancellation(CancellationToken ct)
    {
        // Implementation
    }
}
```

**Generated facade method:**
```csharp
void IAppFacade.CheckCancellation(CancellationToken ct)
{
    ct.ThrowIfCancellationRequested();
    global::MyApp.Utilities.CheckCancellation(ct);
}
```

## Custom Method Naming for Async Methods

Use `AsyncCommandName` and `AsyncQueryName` to customize method names:

```csharp
[FacadeOf(typeof(HandlerAttribute),
    AsyncCommandName = "ExecuteAsync",
    AsyncQueryName = "QueryAsync",
    AsyncStreamName = "StreamAsync")]
public partial interface IAppFacade { }

public class MyService
{
    [Handler]
    public async Task ProcessDataAsync()  // Facade method: ExecuteAsync()
    {
        // Implementation
    }

    [Handler]
    public async Task<string> GetDataAsync()  // Facade method: QueryAsync()
    {
        return await FetchAsync();
    }

    [Handler]
    public async IAsyncEnumerable<Item> GetItemsAsync()  // Facade method: StreamAsync()
    {
        yield return new Item();
    }
}
```

## Best Practices

### 1. Always Use CancellationToken

```csharp
// ✅ Good
[Handler]
public async Task<Data> FetchAsync(string query, CancellationToken ct)
{
    return await _client.GetAsync(query, ct);
}

// ❌ Bad - No cancellation support
[Handler]
public async Task<Data> FetchAsync(string query)
{
    return await _client.GetAsync(query);
}
```

### 2. Use ValueTask for Hot Paths

```csharp
// ✅ Good for frequently-called cached operations
[Handler]
public async ValueTask<User?> GetCachedUserAsync(int id)
{
    if (_cache.TryGetValue(id, out var user))
        return user;  // Synchronous return (no allocation)

    return await _db.Users.FindAsync(id);
}
```

### 3. Prefer Async All the Way

```csharp
// ✅ Good - Async all the way
[Handler]
public async Task<Order> CreateOrderAsync(Order order)
{
    await _db.Orders.AddAsync(order);
    await _db.SaveChangesAsync();
    return order;
}

// ❌ Bad - Blocking on async
[Handler]
public Order CreateOrder(Order order)
{
    _db.Orders.AddAsync(order).Wait();  // Deadlock risk!
    _db.SaveChangesAsync().Wait();
    return order;
}
```

### 4. Use Scoped Facades for DbContext

```csharp
// ✅ Good - Scoped facade for DbContext
[FacadeOf(typeof(HandlerAttribute), Scoped = true)]
public partial interface IOrderFacade { }

public class OrderService
{
    private readonly DbContext _db;

    [Handler]
    public async Task CreateOrderAsync(Order order)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
    }
}

// Usage:
await using var facade = provider.GetRequiredService<IOrderFacade>();
await facade.CreateOrderAsync(order);
```

### 5. Handle Exceptions Properly

```csharp
[Handler]
public async Task<Result> ProcessAsync(string data)
{
    try
    {
        return await _processor.ProcessAsync(data);
    }
    catch (ProcessException ex)
    {
        _logger.LogError(ex, "Processing failed");
        throw;  // Let caller handle
    }
}
```

## Common Async Patterns

### Pattern 1: Async Command with Side Effects

```csharp
[Handler]
public async Task SendNotificationAsync(Notification notification)
{
    await _notificationService.SendAsync(notification);
    await _auditLog.LogAsync("Notification sent");
}
```

### Pattern 2: Async Query with Caching

```csharp
[Handler]
public async ValueTask<User> GetUserAsync(int id, CancellationToken ct = default)
{
    if (_cache.TryGetValue(id, out var user))
        return user;

    user = await _db.Users.FindAsync(id, ct);
    _cache.Set(id, user);
    return user;
}
```

### Pattern 3: Async Stream with Pagination

```csharp
[Handler]
public async IAsyncEnumerable<Item> GetPagedItemsAsync(
    int pageSize = 100,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    int page = 0;
    while (true)
    {
        var items = await _repository.GetPageAsync(page++, pageSize, ct);
        if (!items.Any()) break;

        foreach (var item in items)
            yield return item;
    }
}
```

## Next Steps

- Explore [Advanced Scenarios](../guides/advanced-scenarios.md) for complex async patterns
- Learn about [Testing](../guides/testing.md) async facades
- Check [Troubleshooting](../guides/troubleshooting.md) for async-related issues
