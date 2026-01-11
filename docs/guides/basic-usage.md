# Basic Usage Guide

This guide covers common usage patterns and best practices for Terminus. If you haven't already, check out the [Getting Started](../getting-started.md) guide first.

## Creating a Simple Facade

### Step 1: Define Your Domain Attribute

Choose a descriptive name for your marker attribute:

```csharp
using System;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class HandlerAttribute : Attribute
{
}
```

### Step 2: Create the Facade Interface

```csharp
using Terminus;

[FacadeOf(typeof(HandlerAttribute))]
public partial interface IAppFacade
{
}
```

### Step 3: Mark Your Services

```csharp
public class WeatherService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WeatherService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [Handler]
    public async Task<string> GetWeatherAsync(string city)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetStringAsync($"https://api.weather.com/{city}");
        return response;
    }
}

public class NewsService
{
    private readonly INewsRepository _repository;

    public NewsService(INewsRepository repository)
    {
        _repository = repository;
    }

    [Handler]
    public async Task<List<Article>> GetLatestNewsAsync(int count = 10)
    {
        return await _repository.GetTopAsync(count);
    }
}
```

### Step 4: Register with DI

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register your services
services.AddHttpClient();
services.AddTransient<INewsRepository, NewsRepository>();
services.AddTransient<WeatherService>();
services.AddTransient<NewsService>();

// Register all Terminus facades
services.AddTerminusFacades();

var provider = services.BuildServiceProvider();
```

### Step 5: Use the Facade

```csharp
var facade = provider.GetRequiredService<IAppFacade>();

var weather = await facade.GetWeatherAsync("London");
var news = await facade.GetLatestNewsAsync(5);

Console.WriteLine(weather);
foreach (var article in news)
{
    Console.WriteLine(article.Title);
}
```

## Working with Multiple Services

Facades naturally aggregate methods from multiple services:

```csharp
// User management
public class UserService
{
    [Handler]
    public async Task<User> GetUserAsync(int id) { ... }

    [Handler]
    public async Task CreateUserAsync(User user) { ... }
}

// Order management
public class OrderService
{
    [Handler]
    public async Task<Order> GetOrderAsync(int id) { ... }

    [Handler]
    public async Task PlaceOrderAsync(Order order) { ... }
}

// Product catalog
public class ProductService
{
    [Handler]
    public async Task<List<Product>> SearchProductsAsync(string query) { ... }
}

// All methods available through IAppFacade
var facade = provider.GetRequiredService<IAppFacade>();
var user = await facade.GetUserAsync(123);
var order = await facade.GetOrderAsync(456);
var products = await facade.SearchProductsAsync("laptop");
```

## Class-Level Attributes

Apply the attribute to a class to include all public methods:

```csharp
[Handler]  // All public methods included automatically
public class UserService
{
    public async Task<User> GetAsync(int id) { ... }          // Included
    public async Task CreateAsync(User user) { ... }          // Included
    public async Task DeleteAsync(int id) { ... }             // Included

    private void InternalHelper() { ... }                     // Excluded (private)
}
```

This is useful when:
- All methods in a service should be in the facade
- You want to avoid decorating every method individually
- The service represents a cohesive set of operations

## Organizing Services

### Pattern 1: Feature Folders

Organize services by feature:

```
Features/
├── Users/
│   ├── UserService.cs
│   ├── UserValidator.cs
│   └── UserRepository.cs
├── Orders/
│   ├── OrderService.cs
│   ├── OrderValidator.cs
│   └── OrderRepository.cs
└── Products/
    ├── ProductService.cs
    └── ProductRepository.cs

Facades/
└── IAppFacade.cs  // Aggregates all features
```

### Pattern 2: Domain Boundaries

Separate facades by domain:

```csharp
// User management facade
[FacadeOf(typeof(UserOperationAttribute))]
public partial interface IUserFacade { }

// Order management facade
[FacadeOf(typeof(OrderOperationAttribute))]
public partial interface IOrderFacade { }

// Product catalog facade
[FacadeOf(typeof(CatalogOperationAttribute))]
public partial interface ICatalogFacade { }
```

### Pattern 3: Command-Query Separation

Separate commands (mutations) from queries:

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class CommandAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class QueryAttribute : Attribute { }

[FacadeOf(typeof(CommandAttribute))]
public partial interface ICommands { }

[FacadeOf(typeof(QueryAttribute))]
public partial interface IQueries { }

public class UserService
{
    [Command]
    public async Task CreateUserAsync(User user) { }  // In ICommands

    [Query]
    public async Task<User> GetUserAsync(int id) { }  // In IQueries
}
```

## Working with Generic Methods

Terminus fully supports generic methods:

```csharp
public class DataService
{
    [Handler]
    public async Task<T> GetByIdAsync<T>(int id) where T : class
    {
        return await _repository.FindAsync<T>(id);
    }

    [Handler]
    public async Task<List<T>> SearchAsync<T>(
        Expression<Func<T, bool>> predicate)
        where T : class
    {
        return await _repository.QueryAsync(predicate);
    }
}

// Usage:
var user = await facade.GetByIdAsync<User>(123);
var products = await facade.SearchAsync<Product>(p => p.Price < 100);
```

## Static Methods

Static methods are supported and called directly (no DI lookup):

```csharp
public static class ValidationHelpers
{
    [Handler]
    public static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    [Handler]
    public static string SanitizeInput(string input)
    {
        return input.Trim().Replace("<", "&lt;").Replace(">", "&gt;");
    }
}

// Usage (no service registration required):
bool valid = facade.IsValidEmail("test@example.com");
string clean = facade.SanitizeInput(userInput);
```

## Async Methods

All async patterns are supported:

```csharp
public class AsyncService
{
    // Task (async command)
    [Handler]
    public async Task ProcessAsync(string data)
    {
        await _processor.ProcessAsync(data);
    }

    // Task<T> (async query)
    [Handler]
    public async Task<Result> ComputeAsync(int value)
    {
        return await _calculator.ComputeAsync(value);
    }

    // ValueTask (efficient async command)
    [Handler]
    public async ValueTask CacheAsync(string key, string value)
    {
        await _cache.SetAsync(key, value);
    }

    // ValueTask<T> (efficient async query)
    [Handler]
    public async ValueTask<string?> GetCachedAsync(string key)
    {
        return await _cache.GetAsync(key);
    }

    // IAsyncEnumerable<T> (async streaming)
    [Handler]
    public async IAsyncEnumerable<Item> StreamItemsAsync()
    {
        await foreach (var item in _repository.StreamAsync())
        {
            yield return item;
        }
    }
}
```

## Parameter Types

Terminus supports all parameter types except `ref`, `out`, and `in`:

```csharp
public class ParameterService
{
    // ✅ Supported: Value types
    [Handler]
    public void Process(int id, string name, bool active) { }

    // ✅ Supported: Reference types
    [Handler]
    public void Save(User user, Order order) { }

    // ✅ Supported: Nullable types
    [Handler]
    public void Update(int? id, string? name) { }

    // ✅ Supported: Collections
    [Handler]
    public void Batch(List<int> ids, Dictionary<string, string> metadata) { }

    // ✅ Supported: CancellationToken
    [Handler]
    public async Task ProcessAsync(string data, CancellationToken ct) { }

    // ✅ Supported: Default parameters
    [Handler]
    public void Configure(int timeout = 30, bool verbose = false) { }

    // ❌ NOT supported: ref/out/in parameters
    // [Handler]
    // public void TryGet(int id, out User user) { }  // Compiler error TM0002
}
```

## Viewing Generated Code

To inspect what Terminus generates:

1. Add to your `.csproj`:
   ```xml
   <PropertyGroup>
       <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
   </PropertyGroup>
   ```

2. Build the project

3. Navigate to: `obj/Debug/net8.0/generated/Terminus.Generator/Terminus.Generator.FacadeGenerator/`

4. Open the generated `.g.cs` file

## Best Practices

### 1. Use Descriptive Attribute Names

```csharp
// ✅ Good
public class HttpHandlerAttribute : Attribute { }
public class CommandAttribute : Attribute { }

// ❌ Less clear
public class MyAttribute : Attribute { }
public class ActionAttribute : Attribute { }
```

### 2. Keep Services Focused

```csharp
// ✅ Good - Focused service
public class UserService
{
    [Handler] public Task<User> GetAsync(int id) { }
    [Handler] public Task CreateAsync(User user) { }
    [Handler] public Task DeleteAsync(int id) { }
}

// ❌ Bad - God object
public class MegaService
{
    [Handler] public Task<User> GetUserAsync(int id) { }
    [Handler] public Task<Order> GetOrderAsync(int id) { }
    [Handler] public Task<Product> GetProductAsync(int id) { }
    // 50 more methods...
}
```

### 3. Separate Read and Write Operations

```csharp
[FacadeOf(typeof(CommandAttribute))]
public partial interface ICommands { }

[FacadeOf(typeof(QueryAttribute))]
public partial interface IQueries { }
```

### 4. Use Async All the Way

```csharp
// ✅ Good
[Handler]
public async Task<Data> FetchAsync()
{
    return await _client.GetAsync();
}

// ❌ Bad - Blocking
[Handler]
public Data Fetch()
{
    return _client.GetAsync().Result;  // Deadlock risk!
}
```

### 5. Always Add CancellationToken

```csharp
// ✅ Good
[Handler]
public async Task ProcessAsync(string data, CancellationToken ct = default)
{
    await _processor.ProcessAsync(data, ct);
}

// ❌ Missing cancellation support
[Handler]
public async Task ProcessAsync(string data)
{
    await _processor.ProcessAsync(data);
}
```

## Common Pitfalls

### 1. Forgetting to Register Services

```csharp
// ❌ Forgot to register WeatherService
services.AddTerminusFacades();

var facade = provider.GetRequiredService<IAppFacade>();
facade.GetWeather("London");  // Runtime error: Service not registered

// ✅ Register all dependencies
services.AddTransient<WeatherService>();
services.AddTerminusFacades();
```

### 2. Using ref/out Parameters

```csharp
// ❌ Compiler error TM0002
[Handler]
public bool TryGetUser(int id, out User user) { ... }

// ✅ Use return value or wrapper
[Handler]
public User? TryGetUser(int id) { ... }

[Handler]
public Result<User> GetUser(int id) { ... }
```

### 3. Duplicate Method Signatures

```csharp
public class ServiceA
{
    [Handler]
    public void Process(string data) { }
}

public class ServiceB
{
    [Handler]
    public void Process(string data) { }  // Compiler error TM0001: duplicate signature
}

// ✅ Make signatures unique
public class ServiceA
{
    [Handler]
    public void ProcessA(string data) { }
}

public class ServiceB
{
    [Handler]
    public void ProcessB(string data) { }
}
```

## Next Steps

- Explore [Advanced Scenarios](advanced-scenarios.md) for complex patterns
- Learn about [Testing](testing.md) your facades
- Check [Troubleshooting](troubleshooting.md) for common issues
