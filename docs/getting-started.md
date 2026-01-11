# Getting Started

This guide will walk you through setting up Terminus and creating your first facade.

## Prerequisites

- .NET 8.0 SDK or later
- A C# project (console, web, or library)
- Basic understanding of dependency injection

## Installation

Install Terminus via NuGet:

```bash
dotnet add package Terminus
```

## Your First Facade

### Step 1: Define a Custom Attribute

Create a marker attribute to identify methods that should be included in your facade:

```csharp
using System;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class HandlerAttribute : Attribute
{
}
```

> **Tip:** You can apply this attribute at the method level for fine-grained control, or at the class level to include all public methods.

### Step 2: Create a Facade Interface

Create a `partial interface` and decorate it with the `[FacadeOf]` attribute:

```csharp
using Terminus;

[FacadeOf(typeof(HandlerAttribute))]
public partial interface IAppFacade
{
}
```

The `partial` keyword is required because Terminus will generate the method signatures for you at compile-time.

### Step 3: Mark Your Service Methods

Create services and mark the methods you want to include in the facade:

```csharp
public class WeatherService
{
    [Handler]
    public string GetWeather(string city)
    {
        return $"Weather in {city}: Sunny, 22°C";
    }
}

public class NewsService
{
    [Handler]
    public string GetLatestNews()
    {
        return "Terminus 1.0 released!";
    }
}
```

### Step 4: Register Services with Dependency Injection

Register your services and the generated facade with the DI container:

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register your services
services.AddTransient<WeatherService>();
services.AddTransient<NewsService>();

// Register all Terminus facades (automatically discovers generated implementations)
services.AddTerminusFacades();

var provider = services.BuildServiceProvider();
```

### Step 5: Use the Facade

Inject and use the facade in your application:

```csharp
var facade = provider.GetRequiredService<IAppFacade>();

// Call methods through the facade
Console.WriteLine(facade.GetWeather("London"));
Console.WriteLine(facade.GetLatestNews());
```

## What Just Happened?

When you build your project, Terminus:

1. Discovered your `IAppFacade` interface marked with `[FacadeOf(typeof(HandlerAttribute))]`
2. Found all methods marked with `[Handler]` across your codebase
3. Generated the interface definition with method signatures:
   ```csharp
   public partial interface IAppFacade
   {
       string GetWeather(string city);
       string GetLatestNews();
   }
   ```
4. Generated an implementation class that resolves services from DI and forwards calls:
   ```csharp
   public sealed class IAppFacade_Generated : IAppFacade
   {
       private readonly IServiceProvider _serviceProvider;

       public IAppFacade_Generated(IServiceProvider serviceProvider)
       {
           _serviceProvider = serviceProvider;
       }

       string IAppFacade.GetWeather(string city)
       {
           return _serviceProvider.GetRequiredService<WeatherService>()
               .GetWeather(city);
       }

       string IAppFacade.GetLatestNews()
       {
           return _serviceProvider.GetRequiredService<NewsService>()
               .GetLatestNews();
       }
   }
   ```

All of this happens at **compile-time** with zero runtime reflection!

## Viewing Generated Code

To inspect the generated code:

1. Add this to your `.csproj` file:
   ```xml
   <PropertyGroup>
       <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
   </PropertyGroup>
   ```
2. Build your project
3. Look in `obj/Debug/net8.0/generated/Terminus.Generator/` for the generated files

## Next Steps

- Learn about [core concepts](concepts/facades.md) like facades, attributes, and service resolution
- Explore [advanced scenarios](guides/advanced-scenarios.md) including async methods, scoped facades, and custom naming
- Check out the [examples](examples/basic.md) for real-world usage patterns
