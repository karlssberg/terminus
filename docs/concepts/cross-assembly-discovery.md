# Cross-Assembly Method Discovery

By default, Terminus only discovers methods in the **current compilation** (the project being built). With the `MethodDiscovery` property, you can enable discovery of facade methods from **referenced assemblies**, enabling multi-project facade aggregation.

## MethodDiscoveryMode Enum

```csharp
public enum MethodDiscoveryMode
{
    /// <summary>
    /// Only discover methods from the current compilation (default).
    /// </summary>
    None = 0,

    /// <summary>
    /// Discover methods from directly referenced assemblies only.
    /// Scans assemblies that are explicitly referenced by the current project,
    /// but not their transitive dependencies.
    /// </summary>
    ReferencedAssemblies = 1,

    /// <summary>
    /// Discover methods from all referenced assemblies, including transitive dependencies.
    /// Scans all assemblies available to the compilation.
    /// </summary>
    TransitiveAssemblies = 2
}
```

## Enabling Cross-Assembly Discovery

```csharp
// Discover from directly referenced assemblies only
[FacadeOf<HandlerAttribute>( MethodDiscovery = MethodDiscoveryMode.ReferencedAssemblies)]
public partial interface IHandlers;

// Discover from all assemblies including transitive dependencies
[FacadeOf<HandlerAttribute>( MethodDiscovery = MethodDiscoveryMode.TransitiveAssemblies)]
public partial interface IHandlers;
```

**Backward Compatibility:** The deprecated `IncludeReferencedAssemblies = true` property is still supported and maps to `MethodDiscovery = MethodDiscoveryMode.TransitiveAssemblies`.

When enabled:
- The generator scans assembly references for methods marked with the facade's attribute types
- System assemblies (`System.*`, `Microsoft.*`, `mscorlib`, `netstandard`) are automatically skipped
- Methods from referenced assemblies are merged with local methods before matching and generation

## Discovery Mode Differences

| Mode | What Gets Scanned | Use Case |
|------|-------------------|----------|
| `None` | Current compilation only | Default - single project |
| `ReferencedAssemblies` | Direct project/package references | Controlled discovery |
| `TransitiveAssemblies` | All assemblies including transitive deps | Plugin/modular architectures |

## Multi-Project Architecture

Cross-assembly discovery enables powerful multi-project architectures where handlers are defined across multiple libraries but aggregated into a single facade.

### Project Structure Example

```
Solution/
├── Handlers.Core/           # Core handlers library
│   ├── HandlerAttribute.cs
│   └── CoreHandlers.cs
├── Handlers.Extensions/     # Extended handlers library
│   └── ExtensionHandlers.cs
└── App/                     # Main application
    ├── LocalHandlers.cs
    └── IHandlers.cs         # Facade definition
```

### Implementation

**Handlers.Core** (library project):

```csharp
using System;

namespace Handlers.Core;

// Shared attribute definition
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class HandlerAttribute : Attribute
{
}

// Core handlers
public class CoreHandlers
{
    [Handler]
    public string ProcessCore(string data)
    {
        return $"Core processed: {data}";
    }

    [Handler]
    public async Task<int> ComputeCoreAsync(int value)
    {
        await Task.Delay(10);
        return value * 2;
    }
}
```

**Handlers.Extensions** (library project, references Handlers.Core):

```csharp
using Handlers.Core;

namespace Handlers.Extensions;

public class ExtensionHandlers
{
    [Handler]
    public string ProcessExtension(string data)
    {
        return $"Extension processed: {data}";
    }

    [Handler]
    public void LogExtension(string message)
    {
        Console.WriteLine($"[Extension] {message}");
    }
}
```

**App** (main project, references both):

```csharp
using Terminus;
using Handlers.Core;

namespace App;

// Facade aggregates handlers from ALL referenced assemblies
[FacadeOf<HandlerAttribute>( MethodDiscovery = MethodDiscoveryMode.TransitiveAssemblies)]
public partial interface IHandlers
{
}

// Local handlers are also included
public class LocalHandlers
{
    [Handler]
    public string ProcessLocal(string data)
    {
        return $"Local processed: {data}";
    }
}
```

### Generated Facade

The generated `IHandlers` facade includes methods from all three sources:

```csharp
public partial interface IHandlers
{
    // From Handlers.Core
    string ProcessCore(string data);
    Task<int> ComputeCoreAsync(int value);

    // From Handlers.Extensions
    string ProcessExtension(string data);
    void LogExtension(string message);

    // From App (local)
    string ProcessLocal(string data);
}
```

## Use Cases

### Plugin Architectures

Enable plugin assemblies to contribute handlers that are automatically discovered:

```csharp
// Main application
[FacadeOf<PluginHandlerAttribute>( MethodDiscovery = MethodDiscoveryMode.TransitiveAssemblies)]
public partial interface IPluginHost
{
}

// Plugin assembly (referenced by main app)
public class MyPlugin
{
    [PluginHandler]
    public void OnLoad() { }

    [PluginHandler]
    public void OnUnload() { }
}
```

### Modular Applications

Combine handlers from feature modules into a unified facade:

```csharp
// Features are in separate assemblies
// - Users.Module.dll contains UserHandlers
// - Orders.Module.dll contains OrderHandlers
// - Inventory.Module.dll contains InventoryHandlers

[FacadeOf<FeatureHandlerAttribute>( MethodDiscovery = MethodDiscoveryMode.TransitiveAssemblies)]
public partial interface IFeatureAggregator
{
}

// All feature handlers appear in IFeatureAggregator
```

### Domain-Driven Design

Aggregate handlers from bounded context assemblies:

```csharp
// Each bounded context is in its own assembly
// - Sales.Domain.dll
// - Shipping.Domain.dll
// - Billing.Domain.dll

[FacadeOf<DomainEventAttribute>( MethodDiscovery = MethodDiscoveryMode.TransitiveAssemblies)]
public partial interface IDomainEventBus
{
}
```

### Shared Libraries

Reuse handlers across multiple applications:

```csharp
// Common.Handlers.dll - shared across apps
public class CommonHandlers
{
    [Handler]
    public void LogActivity(string activity) { }

    [Handler]
    public async Task SendNotificationAsync(string message) { }
}

// App1 and App2 both reference Common.Handlers
// Both get the common handlers in their facades automatically
```

## Behavior Details

### Assembly Filtering

Terminus automatically filters which assemblies to scan:

| Assembly Type | Scanned? | Reason |
|--------------|----------|--------|
| `System.*` | No | System assemblies |
| `Microsoft.*` | No | Framework assemblies |
| `mscorlib` | No | Core runtime |
| `netstandard` | No | Standard library |
| `Terminus` | No | Generator assembly |
| Your assemblies | Yes | User assemblies |

### Discovery Scope by Mode

**ReferencedAssemblies:**
- Uses `compilation.Assembly.Modules[0].ReferencedAssemblySymbols`
- Only assemblies directly referenced by your project are scanned
- Dependencies of dependencies are NOT scanned
- Predictable: You control exactly which assemblies are included via project references

**TransitiveAssemblies:**
- Uses `Compilation.References` to get all available assemblies
- Includes transitive dependencies
- Scans everything available to the compilation

### Method Ordering

Methods appear in the generated facade in this order:
1. Local methods (from current compilation) - alphabetically by containing type
2. Referenced assembly methods - alphabetically by assembly name, then by containing type

### Attribute Matching

Cross-assembly attribute matching uses fully-qualified name comparison:

```csharp
// In Assembly A
namespace MyLib;
public class HandlerAttribute : Attribute { }

// In Assembly B (references A)
// Methods marked with MyLib.HandlerAttribute are matched
// even though the symbol comes from a different compilation
```

## Performance Considerations

### Opt-in by Default

The feature is **disabled by default** (`MethodDiscovery = MethodDiscoveryMode.None`):
- Existing projects have zero performance impact
- Only enable when you need cross-assembly discovery

### Compilation Overhead

When enabled:
- Assembly scanning adds to compilation time
- Impact scales with number of referenced assemblies
- System assemblies are pre-filtered (no scanning overhead)

### Recommendations

1. **Choose the right mode**: Use `ReferencedAssemblies` when you only need direct references
2. **Enable selectively**: Only use on facades that need cross-assembly methods
3. **Minimize references**: Only reference assemblies that contain handlers
4. **Use separate facades**: Create different facades for local vs. cross-assembly needs

```csharp
// Local-only facade (fastest compilation)
[FacadeOf<LocalHandlerAttribute>]
public partial interface ILocalHandlers { }

// Direct references only (faster than transitive)
[FacadeOf<SharedHandlerAttribute>( MethodDiscovery = MethodDiscoveryMode.ReferencedAssemblies)]
public partial interface IDirectHandlers { }

// All assemblies (when needed for plugins)
[FacadeOf<PluginHandlerAttribute>( MethodDiscovery = MethodDiscoveryMode.TransitiveAssemblies)]
public partial interface IPluginHandlers { }
```

## Combining with Other Features

### With Method Aggregation

Cross-assembly methods can be aggregated just like local methods:

```csharp
[FacadeOf<NotificationAttribute>( MethodDiscovery = MethodDiscoveryMode.TransitiveAssemblies)]
public partial interface INotificationBus { }

// Handler in Assembly A
public class EmailNotifier
{
    [Notification]
    public void Notify(UserCreated evt) { /* send email */ }
}

// Handler in Assembly B
public class SmsNotifier
{
    [Notification]
    public void Notify(UserCreated evt) { /* send SMS */ }
}

// Both handlers execute when Notify() is called
```

### With Scope Management

Cross-assembly methods work with scope management:

```csharp
[FacadeOf<HandlerAttribute>(CreateScope = true, MethodDiscovery = MethodDiscoveryMode.TransitiveAssemblies)]
public partial interface IScopedHandlers { }
```

### With Custom Naming

Custom method names apply to all discovered methods:

```csharp
[FacadeOf<HandlerAttribute>(
    MethodDiscovery = MethodDiscoveryMode.TransitiveAssemblies,
    CommandName = "Execute",
    QueryName = "Query")]
public partial interface IHandlers { }
```

## Troubleshooting

### Methods Not Discovered

If methods from referenced assemblies aren't appearing:

1. **Check project reference**: Ensure the assembly is directly referenced
2. **Verify discovery mode**: Use `TransitiveAssemblies` for transitive dependencies
3. **Verify attribute type**: The attribute must match (by fully-qualified name)
4. **Check visibility**: Methods must be `public`
5. **Rebuild solution**: Clean and rebuild to ensure generator runs

### Duplicate Signature Errors

If you get TM0001 (duplicate signature) errors:

```csharp
// Assembly A
public class HandlerA
{
    [Handler]
    public void Process(string data) { }
}

// Assembly B
public class HandlerB
{
    [Handler]
    public void Process(string data) { }  // Same signature!
}
```

This is expected behavior - methods are being aggregated. If you don't want aggregation, use different method names or signatures.

## Next Steps

- Learn about [Method Aggregation](aggregation.md) for combining multiple handlers
- Understand [Service Resolution](service-resolution.md) for cross-assembly services
- Explore [Advanced Scenarios](../guides/advanced-scenarios.md) for complex patterns
