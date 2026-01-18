# Strangler Fig Pattern Example

This example demonstrates how Terminus enables the **Strangler Fig Pattern** for gradually migrating legacy monoliths to modern architectures without breaking client code.

## The Strangler Fig Pattern

Named after the strangler fig tree that grows around an existing tree, this pattern gradually replaces legacy functionality:

1. **Wrap** - Create a facade around the legacy system
2. **Migrate** - Move functionality to new implementation piece by piece
3. **Strangle** - Eventually remove legacy code completely

**Key benefit:** Client code remains unchanged during migration.

## Scenario

You have a `LegacyService` that you're migrating to a `ModernService`. Using Terminus, you create a stable `ISystemFacade` that clients depend on, while you incrementally move methods from legacy to modern implementations.

## Complete Code

```csharp
using Microsoft.Extensions.DependencyInjection;
using Terminus;

var services = new ServiceCollection();

// Both legacy and modern services coexist during migration
services.AddTransient<LegacyService>();
services.AddTransient<ModernService>();

// Register Terminus facades
services.AddTerminusFacades();

var serviceProvider = services.BuildServiceProvider();

// The client only knows about ISystemFacade
var facade = serviceProvider.GetRequiredService<ISystemFacade>();

Console.WriteLine("--- Strangler Fig Migration Demo ---");

// 1. Legacy method (not yet migrated)
Console.WriteLine("\n[Client] Requesting legacy data...");
Console.WriteLine(facade.GetLegacyData(123));

// 2. Migrated method (now in ModernService)
// Client code unchanged!
Console.WriteLine("\n[Client] Processing order (migrated)...");
facade.ProcessOrder(456);

// 3. Another migrated method
Console.WriteLine("\n[Client] Checking status...");
Console.WriteLine($"Status: {facade.GetStatus(456)}");

Console.WriteLine("\nMigration in progress: Facade provides stable contract.");

// --- Infrastructure ---

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class StranglerAttribute : Attribute;

[FacadeOf<StranglerAttribute>]
public partial interface ISystemFacade;

// --- Legacy Implementation ---

/// <summary>
/// Represents the legacy monolith being strangled.
/// </summary>
public class LegacyService
{
    [Strangler]
    public string GetLegacyData(int id)
    {
        return $"[LegacyService] Returning old data format for ID {id}";
    }

    // This method was migrated to ModernService:
    // 1. Removed [Strangler] attribute from here
    // 2. Added method to ModernService with [Strangler]
    // 3. Client code unchanged!
}

// --- Modern Implementation ---

/// <summary>
/// Represents the new service replacing the legacy system.
/// </summary>
[Strangler]  // Class-level: all public methods included
public class ModernService
{
    public void ProcessOrder(int orderId)
    {
        Console.WriteLine($"[ModernService] Processing order {orderId} using new engine.");
    }

    public string GetStatus(int orderId)
    {
        return $"Order {orderId} is COMPLETED (via Modern system)";
    }
}
```

## Output

```
--- Strangler Fig Migration Demo ---

[Client] Requesting legacy data...
[LegacyService] Returning old data format for ID 123

[Client] Processing order (migrated)...
[ModernService] Processing order 456 using new engine.

[Client] Checking status...
Status: Order 456 is COMPLETED (via Modern system)

Migration in progress: Facade provides stable contract.
```

## What Terminus Generates

### Partial Interface

```csharp
public partial interface ISystemFacade
{
    string GetLegacyData(int id);     // From LegacyService
    void ProcessOrder(int orderId);   // From ModernService
    string GetStatus(int orderId);    // From ModernService
}
```

### Implementation Class

```csharp
[FacadeImplementation(typeof(global::ISystemFacade))]
public sealed class ISystemFacade_Generated : global::ISystemFacade
{
    private readonly global::System.IServiceProvider _serviceProvider;

    public ISystemFacade_Generated(global::System.IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    string global::ISystemFacade.GetLegacyData(int id)
    {
        // Calls LegacyService
        return global::Microsoft.Extensions.DependencyInjection
            .ServiceProviderServiceExtensions
            .GetRequiredService<LegacyService>(_serviceProvider)
            .GetLegacyData(id);
    }

    void global::ISystemFacade.ProcessOrder(int orderId)
    {
        // Calls ModernService
        global::Microsoft.Extensions.DependencyInjection
            .ServiceProviderServiceExtensions
            .GetRequiredService<ModernService>(_serviceProvider)
            .ProcessOrder(orderId);
    }

    string global::ISystemFacade.GetStatus(int orderId)
    {
        // Calls ModernService
        return global::Microsoft.Extensions.DependencyInjection
            .ServiceProviderServiceExtensions
            .GetRequiredService<ModernService>(_serviceProvider)
            .GetStatus(orderId);
    }
}
```

## Migration Steps

### Phase 1: Wrap Legacy System

```csharp
// All methods in legacy service
[Strangler]
public class LegacyService
{
    public string GetLegacyData(int id) { ... }
    public void ProcessOrder(int orderId) { ... }
    public string GetStatus(int orderId) { ... }
}

// Facade wraps all legacy methods
[FacadeOf<StranglerAttribute>]
public partial interface ISystemFacade;
```

### Phase 2: Introduce Modern Service

```csharp
// Legacy service (unchanged)
[Strangler]
public class LegacyService
{
    public string GetLegacyData(int id) { ... }
    public void ProcessOrder(int orderId) { ... }
    public string GetStatus(int orderId) { ... }
}

// New modern service (empty initially)
[Strangler]
public class ModernService
{
}
```

### Phase 3: Migrate Method by Method

```csharp
// Legacy service - Removed ProcessOrder
[Strangler]
public class LegacyService
{
    public string GetLegacyData(int id) { ... }
    // ProcessOrder removed
    public string GetStatus(int orderId) { ... }
}

// Modern service - Added ProcessOrder
[Strangler]
public class ModernService
{
    public void ProcessOrder(int orderId)  // ✅ Migrated!
    {
        // New implementation
    }
}

// Facade automatically updated - client code unchanged!
```

### Phase 4: Complete Migration

```csharp
// Legacy service - All methods migrated
public class LegacyService
{
    // Empty - can be deleted
}

// Modern service - Has all functionality
[Strangler]
public class ModernService
{
    public string GetLegacyData(int id) { ... }    // ✅ Migrated
    public void ProcessOrder(int orderId) { ... }  // ✅ Migrated
    public string GetStatus(int orderId) { ... }   // ✅ Migrated
}

// Delete LegacyService when ready
```

## Key Benefits

### 1. Zero Client Changes

```csharp
// Client code never changes
var facade = provider.GetRequiredService<ISystemFacade>();
facade.ProcessOrder(123);

// Implementation can be in LegacyService OR ModernService
// Client doesn't know or care!
```

### 2. Incremental Migration

Migrate one method at a time:
- Low risk (small changes)
- Easy rollback (just move attribute back)
- Continuous delivery (deploy after each method)

### 3. Compile-Time Safety

If you forget to migrate a method:

```csharp
// Remove from LegacyService
public class LegacyService
{
    // Removed ProcessOrder
}

// Forget to add to ModernService
public class ModernService
{
    // Oops, forgot ProcessOrder!
}

// Compiler error: ISystemFacade.ProcessOrder not implemented
```

### 4. Coexistence

Legacy and modern code can coexist:

```csharp
[Strangler]
public class LegacyService
{
    public string GetOldData(int id) { ... }  // Still used
}

[Strangler]
public class ModernService
{
    public string GetNewData(int id) { ... }  // Already migrated
}

// Both available through facade during transition
```

## Advanced Patterns

### Feature Flags for Gradual Rollout

```csharp
public class RoutingService
{
    private readonly IFeatureFlags _flags;
    private readonly LegacyService _legacy;
    private readonly ModernService _modern;

    [Strangler]
    public void ProcessOrder(int orderId)
    {
        if (_flags.IsEnabled("UseModernOrderProcessing"))
            _modern.ProcessOrder(orderId);
        else
            _legacy.ProcessOrder(orderId);
    }
}
```

### Adapter Pattern for Interface Mismatch

```csharp
public class LegacyAdapter
{
    private readonly LegacyService _legacy;

    [Strangler]
    public async Task<Order> GetOrderAsync(int id)
    {
        // Adapt legacy sync API to modern async API
        var legacyOrder = _legacy.GetOrder(id);
        return await Task.FromResult(AdaptToModern(legacyOrder));
    }

    private Order AdaptToModern(LegacyOrder legacy)
    {
        // Transform legacy format to modern format
        return new Order { /* mapping */ };
    }
}
```

### Metrics and Monitoring

```csharp
public class MonitoredService
{
    private readonly IMetrics _metrics;
    private readonly LegacyService _legacy;
    private readonly ModernService _modern;

    [Strangler]
    public void ProcessOrder(int orderId)
    {
        var timer = _metrics.StartTimer("ProcessOrder");
        try
        {
            _modern.ProcessOrder(orderId);
            _metrics.Increment("ProcessOrder.Modern");
        }
        finally
        {
            timer.Stop();
        }
    }
}
```

## Best Practices

### 1. Start with Read Operations

Migrate queries before commands:

```csharp
// Phase 1: Migrate reads (low risk)
[Strangler]
public class ModernService
{
    public Order GetOrder(int id) { ... }      // ✅ Read - migrate first
    public List<Order> ListOrders() { ... }   // ✅ Read - migrate first
}

// Phase 2: Migrate writes (higher risk)
[Strangler]
public class ModernService
{
    public void CreateOrder(Order order) { ... }  // ⚠️ Write - migrate later
    public void DeleteOrder(int id) { ... }       // ⚠️ Write - migrate later
}
```

### 2. Use Class-Level Attributes

```csharp
// ✅ Good - Class-level for modern service
[Strangler]
public class ModernService
{
    public void Method1() { }
    public void Method2() { }
    public void Method3() { }
}

// ❌ Bad - Method-level (more work)
public class ModernService
{
    [Strangler] public void Method1() { }
    [Strangler] public void Method2() { }
    [Strangler] public void Method3() { }
}
```

### 3. Keep Both Services Registered

```csharp
// ✅ Good - Both services available during migration
services.AddTransient<LegacyService>();
services.AddTransient<ModernService>();

// ❌ Bad - Removing legacy too early causes runtime errors
services.AddTransient<ModernService>();
// services.AddTransient<LegacyService>();  // Don't remove yet!
```

### 4. Document Migration Status

```csharp
/// <summary>
/// MIGRATION STATUS:
/// ✅ GetOrder - Migrated to ModernService
/// ✅ ListOrders - Migrated to ModernService
/// ⚠️ CreateOrder - In progress
/// ❌ DeleteOrder - Not started
/// </summary>
public class MigrationTracker { }
```

## Running the Example

1. Clone the repository
2. Navigate to `examples/Terminus.Example.StranglerFig/`
3. Run:
   ```bash
   dotnet run
   ```

## Next Steps

- Learn about [Service Resolution](../concepts/service-resolution.md) strategies
- Explore [Advanced Scenarios](../guides/advanced-scenarios.md)
- Read about [Testing](../guides/testing.md) during migrations
