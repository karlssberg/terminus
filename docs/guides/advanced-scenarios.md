# Advanced Scenarios

This guide covers advanced usage patterns for Terminus including scoped facades, custom naming, and complex architectures.

## Scoped Facades

Use `Scoped = true` when methods need to share state (like DbContext):

```csharp
[FacadeOf(typeof(HandlerAttribute), Scoped = true)]
public partial interface IOrderFacade
{
}

public class OrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db) => _db = db;

    [Handler]
    public void CreateOrder(Order order)
    {
        _db.Orders.Add(order);
        _db.SaveChanges();
    }

    [Handler]
    public Order GetOrder(int id)
    {
        // Same DbContext instance as CreateOrder
        return _db.Orders.Find(id);
    }
}

// Usage with disposal
using var facade = provider.GetRequiredService<IOrderFacade>();
facade.CreateOrder(order);
var retrieved = facade.GetOrder(order.Id);
// DbContext disposed here
```

### Async Disposal

For async operations, use `await using`:

```csharp
await using var facade = provider.GetRequiredService<IOrderFacade>();
await facade.CreateOrderAsync(order);
await facade.ProcessPaymentAsync(order.Id);
// Async scope disposed here
```

## Custom Method Naming

Rename facade methods based on return type:

```csharp
[FacadeOf(typeof(HandlerAttribute),
    CommandName = "Execute",           // void methods
    QueryName = "Query",               // T methods
    AsyncCommandName = "ExecuteAsync", // Task methods
    AsyncQueryName = "QueryAsync",     // Task<T> methods
    AsyncStreamName = "Stream")]       // IAsyncEnumerable<T> methods
public partial interface IAppFacade
{
}

public class MyService
{
    [Handler]
    public void DoWork() { }  // Facade: Execute()

    [Handler]
    public string GetData() { }  // Facade: Query()

    [Handler]
    public async Task ProcessAsync() { }  // Facade: ExecuteAsync()

    [Handler]
    public async Task<int> ComputeAsync() { }  // Facade: QueryAsync()

    [Handler]
    public async IAsyncEnumerable<Item> GetItemsAsync() { }  // Facade: Stream()
}
```

## Multiple Attribute Types

Aggregate methods marked with different attributes:

```csharp
public class CommandAttribute : Attribute { }
public class QueryAttribute : Attribute { }
public class EventAttribute : Attribute { }

[FacadeOf(typeof(CommandAttribute), typeof(QueryAttribute), typeof(EventAttribute))]
public partial interface IAppFacade
{
}

public class UserService
{
    [Command]
    public void CreateUser(User user) { }

    [Query]
    public User GetUser(int id) { }

    [Event]
    public void UserCreatedEvent(int userId) { }
}

// All three methods appear in IAppFacade
```

## Generic Methods with Constraints

Full support for generic type parameters and constraints:

```csharp
public class RepositoryService
{
    [Handler]
    public async Task<T> GetByIdAsync<T>(int id)
        where T : class, IEntity, new()
    {
        return await _db.Set<T>().FindAsync(id);
    }

    [Handler]
    public async Task<List<TResult>> QueryAsync<TSource, TResult>(
        Expression<Func<TSource, bool>> predicate,
        Expression<Func<TSource, TResult>> selector)
        where TSource : class
        where TResult : class
    {
        return await _db.Set<TSource>()
            .Where(predicate)
            .Select(selector)
            .ToListAsync();
    }
}

// Usage:
var user = await facade.GetByIdAsync<User>(123);
var names = await facade.QueryAsync<User, string>(
    u => u.IsActive,
    u => u.Name);
```

## Multiple Facades

Create specialized facades for different concerns:

```csharp
// Read operations
[FacadeOf(typeof(QueryAttribute))]
public partial interface IReadFacade { }

// Write operations
[FacadeOf(typeof(CommandAttribute))]
public partial interface IWriteFacade { }

// Background jobs
[FacadeOf(typeof(JobAttribute))]
public partial interface IJobFacade { }

public class UserService
{
    [Query]
    public User GetUser(int id) { }  // IReadFacade

    [Command]
    public void CreateUser(User user) { }  // IWriteFacade

    [Job]
    public void CleanupInactiveUsers() { }  // IJobFacade
}
```

## Attribute Inheritance

Create attribute hierarchies for flexible discovery:

```csharp
// Base attribute
public class OperationAttribute : Attribute { }

// Derived attributes
public class ReadOperationAttribute : OperationAttribute { }
public class WriteOperationAttribute : OperationAttribute { }

// Facade discovers all derived attributes
[FacadeOf(typeof(OperationAttribute))]
public partial interface IAppFacade { }

public class UserService
{
    [ReadOperation]
    public User GetUser(int id) { }  // Included

    [WriteOperation]
    public void SaveUser(User user) { }  // Included
}
```

## Unit of Work Pattern

Combine scoped facades with explicit commits:

```csharp
[FacadeOf(typeof(TransactionAttribute), Scoped = true)]
public partial interface IUnitOfWork
{
}

public class OrderService
{
    private readonly AppDbContext _db;

    [Transaction]
    public void AddOrder(Order order) => _db.Orders.Add(order);

    [Transaction]
    public void AddLineItem(LineItem item) => _db.LineItems.Add(item);

    [Transaction]
    public async Task CommitAsync() => await _db.SaveChangesAsync();
}

// Usage:
await using var uow = provider.GetRequiredService<IUnitOfWork>();
uow.AddOrder(order);
foreach (var item in order.Items)
{
    uow.AddLineItem(item);
}
await uow.CommitAsync();
```

## Strangler Fig Pattern

Gradually replace legacy code:

```csharp
// New attribute for modernized services
public class ModernizedAttribute : Attribute { }

[FacadeOf(typeof(LegacyAttribute), typeof(ModernizedAttribute))]
public partial interface IAppFacade { }

// Legacy service (don't touch)
public class LegacyUserService
{
    [Legacy]
    public User GetUser(int id) => /* old code */;
}

// New modernized service
public class UserService
{
    [Modernized]
    public async Task<User> GetUserAsync(int id) => /* new code */;
}

// Facade contains both methods during migration
// Remove legacy method once all consumers updated
```

## API Gateway Pattern

Aggregate microservices into a unified facade:

```csharp
public class GatewayOperationAttribute : Attribute { }

[FacadeOf(typeof(GatewayOperationAttribute))]
public partial interface IApiGateway { }

// User microservice
public class UserServiceClient
{
    [GatewayOperation]
    public async Task<User> GetUserAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<User>($"/users/{id}");
    }
}

// Order microservice
public class OrderServiceClient
{
    [GatewayOperation]
    public async Task<Order> GetOrderAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<Order>($"/orders/{id}");
    }
}

// Product microservice
public class ProductServiceClient
{
    [GatewayOperation]
    public async Task<Product> GetProductAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<Product>($"/products/{id}");
    }
}

// Single facade for all services
var gateway = provider.GetRequiredService<IApiGateway>();
var user = await gateway.GetUserAsync(123);
var order = await gateway.GetOrderAsync(456);
var product = await gateway.GetProductAsync(789);
```

## Method Aggregation (Notification Pattern)

Multiple handlers can react to the same event automatically through method aggregation:

```csharp
[FacadeOf(typeof(NotificationAttribute))]
public partial interface IEventBus;

// Multiple handlers for the same event - all execute
public class EmailHandler
{
    [Notification]
    public async Task Handle(OrderPlacedEvent evt)
    {
        await _email.SendOrderConfirmationAsync(evt.OrderId, evt.CustomerEmail);
    }
}

public class InventoryHandler
{
    [Notification]
    public async Task Handle(OrderPlacedEvent evt)
    {
        await _inventory.ReserveItemsAsync(evt.Items);
    }
}

public class AnalyticsHandler
{
    [Notification]
    public async Task Handle(OrderPlacedEvent evt)
    {
        await _analytics.TrackAsync("OrderPlaced", evt.OrderId);
    }
}

// Usage: All handlers execute automatically
await eventBus.Handle(new OrderPlacedEvent(
    orderId: 123,
    customerEmail: "customer@example.com",
    items: orderItems
));
```

### Selective Aggregation

Control which return types should be aggregated:

```csharp
// Only aggregate void methods (commands/notifications)
[FacadeOf(typeof(HandlerAttribute),
    AggregationMode = FacadeAggregationMode.Commands)]
public partial interface ICommandBus;

// Aggregate async queries only
[FacadeOf(typeof(HandlerAttribute),
    AggregationMode = FacadeAggregationMode.AsyncQueries)]
public partial interface IQueryBus;

// Multiple flags
[FacadeOf(typeof(HandlerAttribute),
    AggregationMode = FacadeAggregationMode.Commands | FacadeAggregationMode.AsyncQueries)]
public partial interface IHybridBus;
```

**Learn more:** [Method Aggregation](../concepts/aggregation.md)

## MediatR Alternative

Replace MediatR with compile-time safe facades. See the complete [MediatR Alternative Example](../examples/meditr-alternative.md) for details.

```csharp
// Terminus approach - Compile-time safe!
[FacadeOf(typeof(HandlerAttribute))]
public partial interface IMediator { }

public class UserHandlers
{
    [Handler]
    public async Task<User> CreateUser(string name, CancellationToken ct = default)
    {
        // Implementation
    }
}

// Usage - Full IntelliSense, no reflection!
var user = await mediator.CreateUser("John");
```

## Async Streaming with Scope Management

For scoped facades with `IAsyncEnumerable<T>`:

```csharp
[FacadeOf(typeof(HandlerAttribute), Scoped = true)]
public partial interface IDataFacade { }

public class DataService
{
    private readonly AppDbContext _db;

    [Handler]
    public async IAsyncEnumerable<User> StreamUsersAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var user in _db.Users.AsAsyncEnumerable().WithCancellation(ct))
        {
            yield return user;
        }
    }
}

// Usage:
await using var facade = provider.GetRequiredService<IDataFacade>();
await foreach (var user in facade.StreamUsersAsync())
{
    Console.WriteLine(user.Name);
}
// Scope kept alive during enumeration, disposed after
```

## Web API Integration

Use facades in controllers:

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserFacade _facade;

    public UsersController(IUserFacade facade)
    {
        _facade = facade;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var user = await _facade.GetUserAsync(id);
        return user != null ? Ok(user) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] User user)
    {
        await _facade.CreateUserAsync(user);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }
}
```

## Explicit Lifetime Control

Override default facade lifetimes:

```csharp
// Default: Disposable facades → Scoped, others → Transient
services.AddTerminusFacades();

// Explicit lifetime for all facades
services.AddTerminusFacades(ServiceLifetime.Singleton);

// Per-assembly control
services.AddTerminusFacades(ServiceLifetime.Scoped, typeof(IUserFacade).Assembly);
services.AddTerminusFacades(ServiceLifetime.Transient, typeof(IOrderFacade).Assembly);
```

## Next Steps

- Learn about [Testing](testing.md) facades and services
- Check [Troubleshooting](troubleshooting.md) for common issues
- Review [Examples](../examples/basic.md) for real-world patterns
