# Service Resolution

Service resolution is how Terminus obtains instances of your services to invoke methods. Understanding the three resolution strategies helps you design efficient and correct facades.

## Overview

When a facade method is called, Terminus needs to obtain an instance of the service containing the implementation method. The strategy used depends on:
1. Whether the method is **static** or **instance**
2. Whether the facade is configured with **`CreateScope = true`**

## Resolution Strategies

### 1. Static Service Resolution

**Used for:** Static methods

**Behavior:** Direct invocation on the type (no service resolution needed)

```csharp
[FacadeOf<HandlerAttribute>]
public partial interface IAppFacade
{
}

public static class Utilities
{
    [Handler]
    public static void Log(string message)
    {
        Console.WriteLine(message);
    }
}
```

**Generated code:**
```csharp
void IAppFacade.Log(string message)
{
    global::MyApp.Utilities.Log(message);
}
```

**Characteristics:**
- No DI lookup required
- Fastest resolution strategy
- Service doesn't need to be registered in DI container
- Useful for utility methods and pure functions

### 2. Default Service Resolution (No Scope)

**Used for:** Instance methods on facades without scope management (`CreateScope = false` or default)

**Behavior:** Resolves service from root `IServiceProvider` on every invocation

```csharp
[FacadeOf<HandlerAttribute>]  // CreateScope = false (default)
public partial interface IAppFacade
{
}

public class WeatherService
{
    [Handler]
    public string GetWeather(string city)
    {
        return $"Weather in {city}: Sunny";
    }
}
```

**Generated code:**
```csharp
string IAppFacade.GetWeather(string city)
{
    return global::Microsoft.Extensions.DependencyInjection
        .ServiceProviderServiceExtensions
        .GetRequiredService<WeatherService>(_serviceProvider)
        .GetWeather(city);
}
```

**Characteristics:**
- Service resolved on every method call
- Uses root `IServiceProvider`
- Service lifetime determined by its DI registration (Transient, Scoped, Singleton)
- Suitable for stateless facades

**Service Registration:**
```csharp
// Service can be Transient, Scoped, or Singleton
services.AddTransient<WeatherService>();
services.AddTerminusFacades();  // Registers facade as Transient
```

### 3. Scoped Service Resolution (CreateScope = true)

**Used for:** Instance methods on facades with scope management (`CreateScope = true`)

**Behavior:** Creates a DI scope lazily on first use, reuses it for the facade's lifetime, disposes on facade disposal

```csharp
[FacadeOf<HandlerAttribute>(CreateScope = true)]
public partial interface IAppFacade
{
}

public class OrderService
{
    private readonly DbContext _db;

    public OrderService(DbContext db)
    {
        _db = db;
    }

    [Handler]
    public void PlaceOrder(Order order)
    {
        _db.Orders.Add(order);
        _db.SaveChanges();
    }

    [Handler]
    public async Task ProcessPaymentAsync(int orderId)
    {
        // Same DbContext instance as PlaceOrder
        var order = await _db.Orders.FindAsync(orderId);
        // Process payment
    }
}
```

**Generated code:**
```csharp
[FacadeImplementation(typeof(global::MyApp.IAppFacade))]
public sealed class IAppFacade_Generated : global::MyApp.IAppFacade, IDisposable, IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<IServiceScope> _syncScope;
    private readonly Lazy<AsyncServiceScope> _asyncScope;
    private bool _syncDisposed;
    private bool _asyncDisposed;

    public IAppFacade_Generated(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _syncScope = new Lazy<IServiceScope>(() => _serviceProvider.CreateScope());
        _asyncScope = new Lazy<AsyncServiceScope>(() => _serviceProvider.CreateAsyncScope());
    }

    void IAppFacade.PlaceOrder(Order order)
    {
        _syncScope.Value.ServiceProvider
            .GetRequiredService<OrderService>()
            .PlaceOrder(order);
    }

    async Task IAppFacade.ProcessPaymentAsync(int orderId)
    {
        await _asyncScope.Value.ServiceProvider
            .GetRequiredService<OrderService>()
            .ProcessPaymentAsync(orderId)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_syncDisposed || !_syncScope.IsValueCreated) return;
        _syncScope.Value.Dispose();
        _syncDisposed = true;
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_asyncDisposed || !_asyncScope.IsValueCreated) return;
        await _asyncScope.Value.DisposeAsync().ConfigureAwait(false);
        _asyncDisposed = true;
        GC.SuppressFinalize(this);
    }
}
```

**Characteristics:**
- Scope created lazily on first method call
- Separate scopes for sync (`_syncScope`) and async (`_asyncScope`) methods
- Same scope reused for all methods during facade lifetime
- Facade implements `IDisposable` and `IAsyncDisposable`
- Must explicitly dispose the facade to release resources

**Service Registration:**
```csharp
services.AddScoped<DbContext>();
services.AddScoped<OrderService>();
services.AddTerminusFacades();  // Registers facades with scope management as Scoped lifetime
```

**Usage with disposal:**
```csharp
// Sync disposal
using var facade = provider.GetRequiredService<IAppFacade>();
facade.PlaceOrder(order);
// Scope disposed here

// Async disposal (preferred for async methods)
await using var facade = provider.GetRequiredService<IAppFacade>();
await facade.ProcessPaymentAsync(orderId);
// Scope disposed here
```

## Choosing the Right Strategy

### Use Static Resolution When:
- Method is a pure function with no dependencies
- Method only uses static state
- You want maximum performance (no DI lookup)

**Example:**
```csharp
public static class ValidationHelpers
{
    [Handler]
    public static bool IsValidEmail(string email) => /* validation logic */;
}
```

### Use Default Resolution (No Scope) When:
- Services are stateless
- Each method call should use a fresh service instance (Transient)
- Methods don't share state
- Facade is short-lived (controller action, request handler)

**Example:**
```csharp
[FacadeOf<HandlerAttribute>]
public partial interface IApiHandlers { }

public class WeatherService
{
    [Handler]
    public string GetWeather(string city) => /* stateless call */;
}

public class NewsService
{
    [Handler]
    public string GetNews() => /* stateless call */;
}
```

### Use Scoped Resolution (CreateScope = true) When:
- Methods need to share state (e.g., DbContext, Unit of Work)
- You want all method calls within a single facade instance to use the same service instances
- Working with scoped services (database contexts, per-request services)
- Implementing transactional operations across multiple method calls

**Example:**
```csharp
[FacadeOf<HandlerAttribute>(CreateScope = true)]
public partial interface IOrderFacade { }

public class OrderService
{
    private readonly DbContext _db;

    [Handler]
    public void PlaceOrder(Order order)
    {
        _db.Orders.Add(order);
        _db.SaveChanges();
    }

    [Handler]
    public Order GetOrder(int id) => _db.Orders.Find(id);
}
```

## Service Lifetime and Registration

### Facade Lifetimes

The `AddTerminusFacades()` extension method automatically registers facades with appropriate lifetimes:

- **Non-disposable facades** (non-scoped) → `Transient`
- **Disposable facades** (CreateScope = true) → `Scoped`

You can override this:
```csharp
// Explicit lifetime
services.AddTerminusFacades(ServiceLifetime.Singleton);
services.AddTerminusFacades(ServiceLifetime.Scoped);
```

### Underlying Service Lifetimes

Services containing implementation methods should be registered appropriately:

```csharp
// Stateless services → Transient or Singleton
services.AddTransient<WeatherService>();
services.AddSingleton<CacheService>();

// Scoped services → Scoped
services.AddScoped<DbContext>();
services.AddScoped<OrderService>();

// Register facades
services.AddTerminusFacades();
```

## Performance Considerations

### Resolution Cost

1. **Static resolution**: No overhead (direct call)
2. **Non-scoped resolution**: DI lookup on every call
3. **Scoped resolution**: DI lookup on first call, then cached for facade lifetime

### Optimization Tips

**For high-frequency calls:**
```csharp
// Use static methods when possible
public static class FastHelpers
{
    [Handler]
    public static int Calculate(int x, int y) => x + y;
}
```

**For methods that don't share state:**
```csharp
// Use non-scoped (default)
[FacadeOf<HandlerAttribute>]
public partial interface IAppFacade { }
```

**For transactional operations:**
```csharp
// Use scoped to reuse DbContext
[FacadeOf<HandlerAttribute>(CreateScope = true)]
public partial interface IOrderFacade { }
```

## Common Patterns

### Web API Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderFacade _facade;

    public OrdersController(IOrderFacade facade)
    {
        _facade = facade;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] Order order)
    {
        // Facade with CreateScope=true automatically disposed at end of request
        await _facade.PlaceOrderAsync(order);
        return Ok();
    }
}
```

### Unit of Work Pattern

```csharp
[FacadeOf<HandlerAttribute>(CreateScope = true)]
public partial interface IUnitOfWork { }

await using var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
unitOfWork.CreateOrder(order);
unitOfWork.AddLineItem(lineItem);
await unitOfWork.CommitAsync();
// DbContext disposed here
```

## Next Steps

- Learn about [Async Support](async-support.md) for asynchronous methods
- Explore [Advanced Scenarios](../guides/advanced-scenarios.md) for complex patterns
- Check [Troubleshooting](../guides/troubleshooting.md) for common resolution issues
