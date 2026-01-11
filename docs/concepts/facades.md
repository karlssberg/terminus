# Understanding Facades

A **facade** in Terminus is a compile-time generated interface that aggregates methods from multiple services into a single, unified API. This pattern simplifies client code by providing a single entry point while maintaining clean separation of concerns.

## What is a Facade?

In software design, the Facade pattern provides a simplified interface to a complex system. Terminus automates this pattern using source generators, eliminating the need to manually write delegation code.

### Traditional Approach (Manual)

```csharp
public interface IAppFacade
{
    string GetWeather(string city);
    string GetLatestNews();
}

public class AppFacade : IAppFacade
{
    private readonly IServiceProvider _serviceProvider;

    public AppFacade(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string GetWeather(string city)
    {
        // Manual delegation - tedious and error-prone
        return _serviceProvider.GetRequiredService<WeatherService>()
            .GetWeather(city);
    }

    public string GetLatestNews()
    {
        // More boilerplate...
        return _serviceProvider.GetRequiredService<NewsService>()
            .GetLatestNews();
    }
}
```

### Terminus Approach (Generated)

```csharp
// Just define the interface
[FacadeOf(typeof(HandlerAttribute))]
public partial interface IAppFacade
{
}

// Mark your methods
public class WeatherService
{
    [Handler]
    public string GetWeather(string city) { ... }
}

// Terminus generates everything else automatically!
```

## How Facades Work

### 1. Discovery Phase

At compile-time, Terminus scans your codebase for:
- Interfaces marked with `[FacadeOf]`
- Methods or classes marked with your custom attributes

### 2. Generation Phase

For each facade, Terminus generates:

1. **Partial Interface Definition** - Method signatures based on discovered methods
2. **Implementation Class** - Sealed class with explicit interface implementation
3. **Service Resolution Logic** - Code to resolve and invoke methods via DI

### 3. Runtime Behavior

At runtime:
1. The facade is resolved from the DI container
2. When you call a facade method, it resolves the service containing the implementation
3. The call is forwarded to the actual method
4. The result (if any) is returned

## Facade Declaration

### Basic Syntax

```csharp
[FacadeOf(typeof(YourCustomAttribute))]
public partial interface IYourFacade
{
}
```

Key requirements:
- Must be an **interface**
- Must be **partial**
- Must have `[FacadeOf]` attribute with at least one custom attribute type

### Multiple Attributes

You can aggregate methods marked with different attributes into a single facade:

```csharp
[FacadeOf(typeof(CommandAttribute), typeof(QueryAttribute))]
public partial interface IAppFacade
{
}

public class UserService
{
    [Command]
    public void CreateUser(string name) { ... }

    [Query]
    public User GetUser(int id) { ... }
}

// Both methods appear in IAppFacade
```

## Naming and Organization

### Implementation Class Names

Generated implementation classes follow the pattern: `{InterfaceName}_Generated`

```csharp
[FacadeOf(typeof(HandlerAttribute))]
public partial interface IAppFacade { }

// Generates: IAppFacade_Generated
```

### File Names

Generated files follow the pattern: `{Namespace}_{InterfaceName}_Generated.g.cs`

```csharp
namespace MyApp.Services;

[FacadeOf(typeof(HandlerAttribute))]
public partial interface IAppFacade { }

// Generates: MyApp_Services_IAppFacade_Generated.g.cs
```

## Method Signature Generation

Terminus preserves the complete method signature including:
- Return type (including `Task`, `ValueTask`, `IAsyncEnumerable<T>`)
- Method name (or custom name - see [Custom Method Naming](../guides/advanced-scenarios.md#custom-method-naming))
- Parameters (type, name, and order)
- Generic type parameters and constraints

### Example: Complex Signatures

```csharp
public class DataService
{
    [Handler]
    public async Task<IEnumerable<T>> GetDataAsync<T>(
        string query,
        CancellationToken cancellationToken = default)
        where T : class, new()
    {
        // Implementation
    }
}

// Generated facade method:
public partial interface IAppFacade
{
    Task<IEnumerable<T>> GetDataAsync<T>(
        string query,
        CancellationToken cancellationToken = default)
        where T : class, new();
}
```

## Benefits of Compile-Time Facades

### 1. Zero Runtime Overhead
All generation happens at compile-time. No reflection, no runtime discovery, no performance penalty.

### 2. Type Safety
Any breaking changes in your implementation methods are caught at compile-time:

```csharp
// Change a method signature
public class WeatherService
{
    [Handler]
    public string GetWeather(string city, string country) // Added parameter
    {
        ...
    }
}

// Compiler error: facade consumers must update their calls
```

### 3. IntelliSense Support
Generated facades provide full IntelliSense, documentation comments, and go-to-definition support.

### 4. Refactoring-Friendly
Renaming, moving, or restructuring services automatically updates the facade.

### 5. Testability
Easy to mock or replace individual services without affecting the facade contract.

## When to Use Facades

### ✅ Good Use Cases

- **API Gateway Pattern**: Aggregate multiple microservices into a unified API
- **MediatR Alternative**: Replace reflection-based mediators with compile-time facades
- **Strangler Fig Migration**: Gradually replace legacy code without breaking client contracts
- **Feature Modules**: Group related operations from different services
- **UI Backends**: Simplify controller/endpoint code with a single facade

### ❌ When Not to Use

- Single service with no need for aggregation
- Performance-critical hot paths where direct calls are preferred (though Terminus overhead is minimal)
- When you need runtime method discovery or dynamic behavior

## Next Steps

- Learn about [Custom Attributes](attributes.md) for method discovery
- Understand [Service Resolution](service-resolution.md) strategies
- Explore [Async Support](async-support.md) for asynchronous operations
