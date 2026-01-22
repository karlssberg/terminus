# Terminus 🏛️

**Terminus** is a high-performance C# source generator that effortlessly aggregates methods from multiple services into a single, cohesive facade. Stop manually forwarding calls and let the compiler build your API for you.

[![NuGet](https://img.shields.io/nuget/v/Terminus.svg)](https://www.nuget.org/packages/Terminus)

## 🚀 Why Terminus?

Managing large service collections often leads to bloated client code or tedious manual facade implementations. Terminus automates this by "stitching" together methods from disparate types at compile-time.

### Key Use Cases
- **Strongly-typed Mediator:** Replace MediatR with a reflection-free, compile-time safe alternative that provides full IntelliSense.
- **Strangler Fig Pattern:** Gradually migrate legacy monoliths to microservices without breaking client contracts.
- **API Composition:** Combine multiple domain services into a single, unified entry point for UI or external consumers.
- **Decoupling:** Shield consumers from implementation details and service-mesh complexity.

## 🛠️ Quick Start

### 1. Define your Facade
Create a `partial interface` and tag it with a custom attribute.

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class MyFacadeAttribute : Attribute;

[FacadeOf(typeof(MyFacadeAttribute))]
public partial interface IAppFacade;
```

### 2. Tag your Services
Decorate the methods (or entire classes) you want to include in the facade.

```csharp
public class WeatherService {
    [MyFacadeAttribute]
    public string GetWeather() => "Sunny, 22°C";
}

public class NewsService {
    [MyFacadeAttribute]
    public string GetLatestNews() => "Terminus is live!";
}
```

### 3. Register and Inject
Use the auto-generated extension method to register the facade in your DI container.

```csharp
var services = new ServiceCollection();
services.AddTransient<WeatherService>();
services.AddTransient<NewsService>();

// Register all generated facades
services.AddTerminusFacades(); 

var provider = services.BuildServiceProvider();
var facade = provider.GetRequiredService<IAppFacade>();

// Aggregated methods are available directly on the facade
Console.WriteLine(facade.GetWeather());
```

## 📖 Examples

- **[Basic Example](examples/Terminus.Example.Basic):** Simple method aggregation across services.
- **[MediatR Alternative](examples/Terminus.Example.MediatrAlternative):** Create a strongly-typed Mediator without reflection.
- **[Strangler Fig Demo](examples/Terminus.Example.StranglerFig):** See how to migrate legacy code seamlessly.

## 🧩 Features & Configuration

### Method Discovery
Terminus finds methods to include in your facade based on custom marker attributes. You can apply these attributes in two ways:

1.  **Method Level:** Only the decorated method is included.
2.  **Class Level:** All public methods in the class are included in the facade.

```csharp
[MyMarker]
public class UserService {
    public void Create() { ... } // Included
    public void Delete() { ... } // Included
}

public class OrderService {
    [MyMarker]
    public void Place() { ... } // Included
    public void Cancel() { ... } // NOT included
}
```

### Method Naming Overrides
By default, Terminus uses the original method names. You can override these names globally on the facade interface using the `[FacadeOf]` attribute. This is particularly useful for creating consistent APIs (e.g., renaming all `void` methods to `Publish` or `Execute`).

| Property | Description | Default |
| :--- | :--- | :--- |
| `CommandName` | Renames methods returning `void`. | Original Name |
| `QueryName` | Renames methods returning a value. | Original Name |
| `AsyncCommandName` | Renames methods returning `Task` or `ValueTask`. | Original Name |
| `AsyncQueryName` | Renames methods returning `Task<T>` or `ValueTask<T>`. | Original Name |
| `AsyncStreamName` | Renames methods returning `IAsyncEnumerable<T>`. | Original Name |

**Example:**
```csharp
[FacadeOf(typeof(MyMarker), CommandName = "Execute", QueryName = "Fetch")]
public partial interface IAppFacade;
```

### Service Lifetimes & Scoping
By default, services are resolved per method invocation from the root `IServiceProvider`. If you need the facade to create and manage its own service scope (useful in web applications or unit-of-work patterns), set the `CreateScope` property to `true`.

```csharp
[FacadeOf(typeof(MyMarker), CreateScope = true)]
public partial interface IScopedFacade;
```

### Multiple Marker Attributes
A single facade can aggregate methods marked with different attributes. This allows you to compose a facade from multiple logical groups of services.

```csharp
[FacadeOf(typeof(InventoryMarker), typeof(ShippingMarker))]
public partial interface ILogisticsFacade;
```

### Dependency Injection
Terminus provides a convenient extension method to register all generated facades and their implementations into the `IServiceCollection`.

```csharp
services.AddTerminusFacades();
```

> **Note:** You still need to register your underlying services (the ones containing the marker attributes) in the DI container manually.

### Async & Streams Support
Terminus natively supports asynchronous programming patterns:
- **Async Methods:** Methods returning `Task`, `ValueTask`, `Task<T>`, or `ValueTask<T>` are generated with correct `await` logic.
- **Async Streams:** Methods returning `IAsyncEnumerable<T>` are correctly forwarded, allowing you to stream data through the facade.

## 📜 License
MIT