# Terminus 🏛️

**Terminus** is a high-performance C# source generator that effortlessly aggregates methods from multiple services into a single, cohesive facade. Stop manually forwarding calls and let the compiler build your API for you.

## 🚀 Quick Start

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

## 🧩 Key Features

- **Compile-time code generation**: Uses Roslyn source generators for zero runtime overhead
- **Type-safe facades**: Generates strongly-typed facade interfaces with full IntelliSense support
- **Flexible service resolution**: Supports static methods, scoped instances, and non-scoped instances
- **Async support**: Full support for `Task`, `ValueTask`, `Task<T>`, `ValueTask<T>`, and `IAsyncEnumerable<T>`
- **Generic method support**: Full support for generic methods with type parameters and constraints
- **Custom method naming**: Configure different method names based on return types (Command, Query, etc.)
- **Dependency injection integration**: Seamless integration with Microsoft.Extensions.DependencyInjection

## 📖 Documentation

For complete documentation, examples, and advanced usage, visit the [GitHub repository](https://github.com/karlssberg/terminus).

## 📜 License

MIT
