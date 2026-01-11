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

```csharp
// 1. Define your custom attribute
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class MyFacadeAttribute : Attribute;

// 2. Mark interface with [FacadeOf]
[FacadeOf(typeof(MyFacadeAttribute))]
public partial interface IAppFacade;

// 3. Mark your service methods
public class WeatherService {
    [MyFacadeAttribute]
    public string GetWeather() => "Sunny, 22°C";
}

// 4. Register and use
services.AddTerminusFacades();
var facade = provider.GetRequiredService<IAppFacade>();
facade.GetWeather(); // ✨ Generated at compile-time
```

## 📖 Documentation

<div class="row">
  <div class="col-md-6">
    <div class="card">
      <div class="card-body">
        <h5 class="card-title">Getting Started</h5>
        <p class="card-text">Learn the basics and set up your first facade.</p>
        <a href="docs/getting-started.html" class="btn btn-primary">Get Started</a>
      </div>
    </div>
  </div>
  <div class="col-md-6">
    <div class="card">
      <div class="card-body">
        <h5 class="card-title">Core Concepts</h5>
        <p class="card-text">Understand facades, attributes, and service resolution.</p>
        <a href="docs/concepts/facades.html" class="btn btn-primary">Learn Concepts</a>
      </div>
    </div>
  </div>
</div>

<div class="row">
  <div class="col-md-6">
    <div class="card">
      <div class="card-body">
        <h5 class="card-title">Guides & Tutorials</h5>
        <p class="card-text">Step-by-step guides for common scenarios.</p>
        <a href="docs/guides/basic-usage.html" class="btn btn-primary">View Guides</a>
      </div>
    </div>
  </div>
  <div class="col-md-6">
    <div class="card">
      <div class="card-body">
        <h5 class="card-title">API Reference</h5>
        <p class="card-text">Complete API documentation with examples.</p>
        <a href="api/Terminus.html" class="btn btn-primary">API Docs</a>
      </div>
    </div>
  </div>
</div>

## 🧩 Key Features

- **Compile-time code generation**: Uses Roslyn source generators for zero runtime overhead
- **Type-safe facades**: Generates strongly-typed facade interfaces with full IntelliSense support
- **Flexible service resolution**: Supports static methods, scoped instances, and non-scoped instances
- **Async support**: Full support for `Task`, `ValueTask`, `Task<T>`, `ValueTask<T>`, and `IAsyncEnumerable<T>`
- **Generic method support**: Full support for generic methods with type parameters and constraints
- **Custom method naming**: Configure different method names based on return types (Command, Query, etc.)
- **Dependency injection integration**: Seamless integration with Microsoft.Extensions.DependencyInjection

## 📦 Installation

Install via NuGet Package Manager:

```bash
dotnet add package Terminus
```

Or via Package Manager Console:

```powershell
Install-Package Terminus
```

## 📜 License

MIT License - see [LICENSE](https://github.com/karlssberg/terminus/blob/main/LICENSE) for details.
