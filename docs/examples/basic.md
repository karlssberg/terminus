# Basic Example

This example demonstrates the fundamental usage of Terminus: aggregating methods from multiple services into a single facade interface.

## Scenario

You have three independent services (`GreetingService`, `WeatherService`, `NewsService`) and want to expose their methods through a unified `IAppFacade` interface without manually writing delegation code.

## Complete Code

```csharp
using Microsoft.Extensions.DependencyInjection;
using Terminus;

var services = new ServiceCollection();

// 1. Register the underlying services
services.AddTransient<GreetingService>();
services.AddTransient<WeatherService>();
services.AddTransient<NewsService>();

// 2. Register all Terminus facades
services.AddTerminusFacades();

var serviceProvider = services.BuildServiceProvider();

// 3. Resolve and use the facade
var facade = serviceProvider.GetRequiredService<IAppFacade>();

facade.SayHello("Developer");
Console.WriteLine(facade.GetWeather());
Console.WriteLine(facade.GetLatestNews());

// --- Definitions ---

// Define a custom marker attribute
[AttributeUsage(AttributeTargets.Method)]
public class FacadeMethodAttribute : Attribute;

// Mark interface for facade generation
[FacadeOf(typeof(FacadeMethodAttribute))]
public partial interface IAppFacade;

// Service implementations
public class GreetingService
{
    [FacadeMethod]
    public void SayHello(string name) => Console.WriteLine($"Hello, {name}!");
}

public class WeatherService
{
    [FacadeMethod]
    public string GetWeather() => "The weather is sunny and 22°C.";
}

public class NewsService
{
    [FacadeMethod]
    public string GetLatestNews() => "Terminus simplifies facade generation!";
}
```

## Output

```
Hello, Developer!
The weather is sunny and 22°C.
Terminus simplifies facade generation!
```

## What Terminus Generates

### Partial Interface

```csharp
public partial interface IAppFacade
{
    void SayHello(string name);
    string GetWeather();
    string GetLatestNews();
}
```

### Implementation Class

```csharp
[FacadeImplementation(typeof(global::IAppFacade))]
public sealed class IAppFacade_Generated : global::IAppFacade
{
    private readonly global::System.IServiceProvider _serviceProvider;

    public IAppFacade_Generated(global::System.IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    void global::IAppFacade.SayHello(string name)
    {
        global::Microsoft.Extensions.DependencyInjection
            .ServiceProviderServiceExtensions
            .GetRequiredService<GreetingService>(_serviceProvider)
            .SayHello(name);
    }

    string global::IAppFacade.GetWeather()
    {
        return global::Microsoft.Extensions.DependencyInjection
            .ServiceProviderServiceExtensions
            .GetRequiredService<WeatherService>(_serviceProvider)
            .GetWeather();
    }

    string global::IAppFacade.GetLatestNews()
    {
        return global::Microsoft.Extensions.DependencyInjection
            .ServiceProviderServiceExtensions
            .GetRequiredService<NewsService>(_serviceProvider)
            .GetLatestNews();
    }
}
```

## Key Concepts Demonstrated

### 1. Custom Marker Attribute

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class FacadeMethodAttribute : Attribute;
```

This attribute marks methods for inclusion in the facade. You can name it anything you want.

### 2. Facade Interface

```csharp
[FacadeOf(typeof(FacadeMethodAttribute))]
public partial interface IAppFacade;
```

The `[FacadeOf]` attribute triggers source generation. The `partial` keyword is required.

### 3. Marking Methods

```csharp
[FacadeMethod]
public void SayHello(string name) { ... }
```

Apply your custom attribute to methods you want in the facade.

### 4. Service Registration

```csharp
services.AddTransient<GreetingService>();
services.AddTerminusFacades();  // Registers all facades
```

Register your services and facades with the DI container.

### 5. Usage

```csharp
var facade = serviceProvider.GetRequiredService<IAppFacade>();
facade.SayHello("Developer");
```

Use the facade like any other interface.

## Benefits

### ✅ No Manual Delegation

You don't need to write this:

```csharp
public class AppFacade : IAppFacade
{
    private readonly IServiceProvider _serviceProvider;

    public void SayHello(string name)
    {
        _serviceProvider.GetRequiredService<GreetingService>().SayHello(name);
    }

    public string GetWeather()
    {
        return _serviceProvider.GetRequiredService<WeatherService>().GetWeather();
    }

    // ... more boilerplate
}
```

### ✅ Compile-Time Safety

If you change a method signature:

```csharp
[FacadeMethod]
public string GetWeather(string city)  // Added parameter
{
    return $"Weather in {city}: Sunny";
}
```

The facade automatically updates, and callers get compile errors if they don't update their calls.

### ✅ Full IntelliSense

All facade methods show up with full IntelliSense, documentation comments, and parameter info.

### ✅ Zero Runtime Overhead

Everything is generated at compile-time. No reflection, no runtime discovery.

## Variations

### Class-Level Attributes

Instead of marking every method, mark the entire class:

```csharp
[FacadeMethod]  // All public methods included
public class NewsService
{
    public string GetLatestNews() => "Breaking news!";
    public string GetHeadline() => "Major announcement!";
    public int GetArticleCount() => 42;
}

// All three methods appear in IAppFacade
```

### Multiple Services, Same Method Names

Methods from different services can have the same name:

```csharp
public class UserService
{
    [FacadeMethod]
    public void Save(User user) { ... }
}

public class OrderService
{
    [FacadeMethod]
    public void Save(Order order) { ... }  // Different parameter type = unique signature
}

// Both methods appear in facade:
facade.Save(user);   // Calls UserService.Save
facade.Save(order);  // Calls OrderService.Save
```

### Async Methods

Terminus fully supports async methods:

```csharp
public class WeatherService
{
    [FacadeMethod]
    public async Task<string> GetWeatherAsync(string city)
    {
        await Task.Delay(100);  // Simulate API call
        return $"Weather in {city}: Sunny";
    }
}

// Usage:
var weather = await facade.GetWeatherAsync("London");
```

## Running the Example

1. Clone the repository
2. Navigate to `examples/Terminus.Example.Basic/`
3. Run:
   ```bash
   dotnet run
   ```

## Next Steps

- Try the [MediatR Alternative](meditr-alternative.md) example
- Explore the [Strangler Fig Pattern](strangler-fig.md) example
- Read the [Getting Started](../getting-started.md) guide
