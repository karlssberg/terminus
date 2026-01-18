# Method Aggregation

Method aggregation is a powerful feature in Terminus that automatically combines multiple methods with identical signatures into a single facade method. This enables notification/broadcast patterns similar to MediatR's `INotification` handlers.

## Overview

When multiple methods in your codebase share the same signature (name, parameters, and generic constraints), Terminus automatically aggregates them into a single facade method that executes all handlers in sequence.

**Key Benefits:**
- **Notification Pattern**: Multiple handlers react to the same event (audit, logging, email)
- **Multi-Source Queries**: Collect results from multiple data sources (cache + database)
- **Side Effects**: Trigger multiple independent actions without coupling
- **Observability**: Multiple observers monitoring the same action

## Default Behavior

By default, methods with identical signatures are automatically aggregated. This behavior works without any configuration.

### Void Methods (Notifications)

When multiple void methods share the same signature, they all execute in sequence:

```csharp
[FacadeOf<HandlerAttribute>]
public partial interface INotificationBus;

public class HandlerAttribute : Attribute { }

// Multiple handlers for the same notification
public class EmailNotificationHandler
{
    [Handler]
    public void Handle(UserCreatedNotification notification)
    {
        Console.WriteLine($"Sending email to: {notification.Email}");
    }
}

public class LoggingNotificationHandler
{
    [Handler]
    public void Handle(UserCreatedNotification notification)
    {
        Console.WriteLine($"Logging user creation: {notification.UserId}");
    }
}

public class AuditNotificationHandler
{
    [Handler]
    public void Handle(UserCreatedNotification notification)
    {
        Console.WriteLine($"Auditing user creation: {notification.UserId}");
    }
}

public record UserCreatedNotification(int UserId, string Email);
```

**Generated Interface:**
```csharp
public partial interface INotificationBus
{
    void Handle(UserCreatedNotification notification);
}
```

**Usage:**
```csharp
// All three handlers execute automatically
notificationBus.Handle(new UserCreatedNotification(123, "user@example.com"));

// Output:
// Sending email to: user@example.com
// Logging user creation: 123
// Auditing user creation: 123
```

### Result Methods (Queries)

When methods return results, the aggregated method returns `IEnumerable<T>` to collect all results:

```csharp
[FacadeOf<HandlerAttribute>]
public partial interface ISearchBus;

public class PrimarySearchHandler
{
    [Handler]
    public SearchResult Search(string query)
    {
        return new SearchResult("Primary Source", score: 10);
    }
}

public class SecondarySearchHandler
{
    [Handler]
    public SearchResult Search(string query)
    {
        return new SearchResult("Secondary Source", score: 5);
    }
}

public record SearchResult(string Source, int Score);
```

**Generated Interface:**
```csharp
public partial interface ISearchBus
{
    IEnumerable<SearchResult> Search(string query);
}
```

**Usage:**
```csharp
// Collect results from all search handlers
var results = searchBus.Search("term")
    .OrderByDescending(r => r.Score)
    .ToList();

// Results contains items from both handlers
foreach (var result in results)
{
    Console.WriteLine($"{result.Source}: Score {result.Score}");
}

// Output:
// Primary Source: Score 10
// Secondary Source: Score 5
```

### Async Result Methods

For async methods returning `Task<T>` or `ValueTask<T>`, the aggregated method returns `IAsyncEnumerable<T>`:

```csharp
[FacadeOf<HandlerAttribute>]
public partial interface IAsyncQueryBus;

public class DatabaseSearchHandler
{
    [Handler]
    public async Task<User> SearchAsync(string query)
    {
        await Task.Delay(10); // Simulate database query
        return new User(1, "DB Result");
    }
}

public class CacheSearchHandler
{
    [Handler]
    public async Task<User> SearchAsync(string query)
    {
        await Task.Delay(5); // Simulate cache lookup
        return new User(2, "Cache Result");
    }
}

public record User(int Id, string Name);
```

**Generated Interface:**
```csharp
public partial interface IAsyncQueryBus
{
    IAsyncEnumerable<User> SearchAsync(string query);
}
```

**Usage:**
```csharp
// Stream results as they become available
await foreach (var user in asyncQueryBus.SearchAsync("john"))
{
    Console.WriteLine($"Found: {user.Name}");
}

// Output (order may vary):
// Found: Cache Result
// Found: DB Result
```

## Return Type Transformations

When methods are aggregated, return types are transformed to accommodate multiple results:

| Original Return Type | Aggregated Return Type | Behavior |
|---------------------|------------------------|----------|
| `void` | `void` | Executes all handlers in sequence |
| `T` | `IEnumerable<T>` | Yields result from each handler |
| `Task` | `Task` | Awaits all handlers in sequence |
| `ValueTask` | `Task` | Awaits all handlers in sequence |
| `Task<T>` | `IAsyncEnumerable<T>` | Yields awaited result from each handler |
| `ValueTask<T>` | `IAsyncEnumerable<T>` | Yields awaited result from each handler |
| `IAsyncEnumerable<T>` | N/A | Not aggregated (streaming not composable) |

## Selective Aggregation

The `AggregationMode` property provides fine-grained control over which return types should be aggregated.

### Available Flags

```csharp
public enum FacadeAggregationMode
{
    None = 0,              // Default: aggregate all matching signatures
    Commands = 1 << 0,     // Aggregate void methods only
    Queries = 1 << 1,      // Aggregate result (T) methods only
    AsyncCommands = 1 << 2,    // Aggregate Task/ValueTask methods only
    AsyncQueries = 1 << 3,     // Aggregate Task<T>/ValueTask<T> methods only
    AsyncStreams = 1 << 4,     // Aggregate IAsyncEnumerable<T> methods only
    All = Commands | Queries | AsyncCommands | AsyncQueries | AsyncStreams
}
```

### Aggregate Only Commands

Prevent accidental aggregation of query results while still enabling notification patterns:

```csharp
[FacadeOf<HandlerAttribute>(
    AggregationMode = FacadeAggregationMode.Commands)]
public partial interface ICommandBus;

public class UserHandlers
{
    [Handler]
    public void CreateUser(CreateUserCommand cmd) { }  // Will be aggregated

    [Handler]
    public void DeleteUser(DeleteUserCommand cmd) { }  // Will be aggregated

    [Handler]
    public string GetUser(GetUserQuery query) { }  // Separate method (not void)
}
```

### Combine Multiple Flags

Control aggregation for multiple return types:

```csharp
[FacadeOf<HandlerAttribute>(
    AggregationMode = FacadeAggregationMode.Commands | FacadeAggregationMode.AsyncQueries)]
public partial interface IHybridBus;

// Only void methods and Task<T> methods will be aggregated
// Other return types generate separate facade methods
```

### Disable Aggregation for Specific Types

```csharp
// Aggregate everything EXCEPT queries (result methods)
[FacadeOf<HandlerAttribute>(
    AggregationMode = FacadeAggregationMode.Commands |
                     FacadeAggregationMode.AsyncCommands |
                     FacadeAggregationMode.AsyncQueries)]
public partial interface IMediator;
```

## Real-World Examples

### Multi-Channel Notifications

Send notifications through multiple channels (email, SMS, push) automatically:

```csharp
[FacadeOf<NotificationAttribute>]
public partial interface INotificationService;

public class EmailNotifier
{
    [Notification]
    public async Task Send(OrderShippedEvent evt)
    {
        await _emailService.SendAsync(evt.CustomerEmail,
            "Your order has shipped!",
            $"Tracking: {evt.TrackingNumber}");
    }
}

public class SmsNotifier
{
    [Notification]
    public async Task Send(OrderShippedEvent evt)
    {
        await _smsService.SendAsync(evt.CustomerPhone,
            $"Order shipped! Track: {evt.TrackingNumber}");
    }
}

public class PushNotifier
{
    [Notification]
    public async Task Send(OrderShippedEvent evt)
    {
        await _pushService.SendAsync(evt.CustomerId,
            "Order Shipped",
            $"Your order is on its way! Track: {evt.TrackingNumber}");
    }
}

// Usage: All channels notified automatically
await notificationService.Send(new OrderShippedEvent(
    customerId: 123,
    customerEmail: "customer@example.com",
    customerPhone: "+1234567890",
    trackingNumber: "1Z999AA10123456784"
));
```

### Federated Search

Search multiple data sources and combine results:

```csharp
[FacadeOf<SearchAttribute>]
public partial interface IFederatedSearch;

public class DatabaseSearch
{
    [Search]
    public async Task<IEnumerable<Product>> FindProducts(string query)
    {
        return await _db.Products
            .Where(p => p.Name.Contains(query))
            .Take(10)
            .ToListAsync();
    }
}

public class ElasticsearchSearch
{
    [Search]
    public async Task<IEnumerable<Product>> FindProducts(string query)
    {
        return await _elastic.SearchAsync<Product>(query, limit: 10);
    }
}

public class CacheSearch
{
    [Search]
    public async Task<IEnumerable<Product>> FindProducts(string query)
    {
        if (await _cache.TryGetAsync<IEnumerable<Product>>(query, out var cached))
            return cached;
        return Enumerable.Empty<Product>();
    }
}

// Usage: Results from all sources
await foreach (var product in federatedSearch.FindProducts("laptop"))
{
    Console.WriteLine($"{product.Name} - ${product.Price}");
}
```

### Event Sourcing

Multiple projections updating from the same event:

```csharp
[FacadeOf<ProjectionAttribute>(
    AggregationMode = FacadeAggregationMode.AsyncCommands)]
public partial interface IProjectionEngine;

public class ReadModelProjection
{
    [Projection]
    public async Task Project(UserRegisteredEvent evt)
    {
        await _readDb.Users.AddAsync(new UserReadModel
        {
            Id = evt.UserId,
            Email = evt.Email
        });
        await _readDb.SaveChangesAsync();
    }
}

public class AnalyticsProjection
{
    [Projection]
    public async Task Project(UserRegisteredEvent evt)
    {
        await _analytics.TrackEvent("UserRegistered", new
        {
            UserId = evt.UserId,
            Timestamp = evt.RegisteredAt
        });
    }
}

public class NotificationProjection
{
    [Projection]
    public async Task Project(UserRegisteredEvent evt)
    {
        await _notifications.QueueWelcomeEmailAsync(evt.UserId);
    }
}

// Usage: All projections update automatically
await projectionEngine.Project(new UserRegisteredEvent(
    userId: 123,
    email: "newuser@example.com",
    registeredAt: DateTime.UtcNow
));
```

## When to Use Aggregation

### Good Use Cases

- ✅ **Notification/Event Patterns**: Multiple independent handlers reacting to the same event
- ✅ **Multi-Source Queries**: Collecting results from different data sources (cache, DB, API)
- ✅ **Side Effects**: Triggering multiple actions (logging, analytics, webhooks)
- ✅ **Observability**: Multiple monitoring/auditing systems
- ✅ **CQRS Event Handlers**: Multiple read models updating from the same write event
- ✅ **Federated Search**: Searching across multiple indexes/databases

### When to Avoid

- ❌ **Order-Critical Operations**: When execution order matters critically (aggregation order is alphabetical by type name)
- ❌ **Single Handler Expected**: When only one handler should execute
- ❌ **Different Purposes**: When handlers have fundamentally different purposes despite same signature (use different attributes)
- ❌ **Performance Critical**: When you need fine-grained control over execution

## Best Practices

### 1. Use Descriptive Notification Types

```csharp
// Good: Clear intent
public record UserCreatedNotification(int UserId, string Email, DateTime CreatedAt);

// Bad: Generic, unclear
public record NotificationData(int Id, string Data);
```

### 2. Keep Handlers Independent

Each handler should be independent and not rely on other handlers executing first or last:

```csharp
// Good: Independent handlers
public class EmailHandler
{
    [Notification]
    public async Task Handle(OrderPlacedEvent evt)
    {
        // Self-contained: sends email
        await _email.SendOrderConfirmationAsync(evt.OrderId);
    }
}

public class InventoryHandler
{
    [Notification]
    public async Task Handle(OrderPlacedEvent evt)
    {
        // Self-contained: updates inventory
        await _inventory.ReserveItemsAsync(evt.Items);
    }
}
```

### 3. Handle Errors Gracefully

Consider wrapping handlers in try-catch to prevent one failure from blocking others:

```csharp
public class ResilientEmailHandler
{
    [Notification]
    public async Task Handle(UserCreatedEvent evt)
    {
        try
        {
            await _email.SendWelcomeAsync(evt.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email for user {UserId}", evt.UserId);
            // Don't rethrow - let other handlers continue
        }
    }
}
```

### 4. Use Selective Aggregation for Mixed Scenarios

When some methods should aggregate and others shouldn't:

```csharp
// Only aggregate notifications (void methods)
[FacadeOf<HandlerAttribute>(
    AggregationMode = FacadeAggregationMode.Commands)]
public partial interface IApplicationBus;

public class Handlers
{
    // These will aggregate
    [Handler]
    public void NotifyUserCreated(UserCreatedEvent evt) { }

    [Handler]
    public void NotifyUserDeleted(UserDeletedEvent evt) { }

    // These won't aggregate (different signatures mean separate methods anyway)
    [Handler]
    public User GetUser(int id) { }

    [Handler]
    public Order GetOrder(int id) { }
}
```

## See Also

- [MediatR Alternative Example](../examples/meditr-alternative.md) - Building a type-safe mediator
- [Service Resolution](service-resolution.md) - Understanding how services are resolved
- [Async Support](async-support.md) - Working with async patterns
- [Advanced Scenarios](../guides/advanced-scenarios.md) - Complex usage patterns
