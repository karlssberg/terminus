# Custom Attributes

Custom attributes are the mechanism Terminus uses to discover methods for inclusion in facades. By defining your own marker attributes, you control which methods are aggregated and create domain-specific facades.

## Defining Custom Attributes

### Basic Attribute

The simplest custom attribute is an empty class inheriting from `Attribute`:

```csharp
using System;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class HandlerAttribute : Attribute
{
}
```

**Key points:**
- Use `AttributeUsage` to specify where the attribute can be applied
- `AttributeTargets.Method` allows method-level application
- `AttributeTargets.Class` allows class-level application (includes all public methods)

### Attribute with Metadata

You can add properties to store metadata about methods:

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class HttpHandlerAttribute : Attribute
{
    public string Route { get; }
    public string Method { get; set; } = "GET";

    public HttpHandlerAttribute(string route)
    {
        Route = route;
    }
}
```

Usage:
```csharp
public class UserController
{
    [HttpHandler("/users/{id}", Method = "GET")]
    public User GetUser(int id) { ... }

    [HttpHandler("/users", Method = "POST")]
    public void CreateUser(User user) { ... }
}
```

> **Note:** Terminus includes methods based on attribute presence. Metadata is not used by the generator but is available for inspection at runtime.

## Applying Attributes

### Method-Level Application

Apply the attribute to specific methods you want to include in the facade:

```csharp
public class OrderService
{
    [Handler]
    public void PlaceOrder(Order order)
    {
        // This method will be included in the facade
    }

    public void CancelOrder(int orderId)
    {
        // This method will NOT be included (no attribute)
    }
}
```

**Characteristics:**
- Fine-grained control over which methods are included
- Explicit and clear intent
- Useful when only a subset of methods should be exposed

### Class-Level Application

Apply the attribute to the entire class to include **all public methods**:

```csharp
[Handler]
public class OrderService
{
    public void PlaceOrder(Order order)
    {
        // Included
    }

    public void CancelOrder(int orderId)
    {
        // Included
    }

    public Order GetOrder(int orderId)
    {
        // Included
    }

    private void InternalHelper()
    {
        // NOT included (private methods are excluded)
    }
}
```

**Characteristics:**
- Includes all public instance and static methods
- Excludes private, protected, and internal methods
- Excludes special methods (constructors, property accessors, operators, finalizers, etc.)
- Convenient for services where all public methods should be in the facade

### Mixed Application

You can combine both approaches:

```csharp
[Handler]  // Class-level: includes all public methods
public class OrderService
{
    public void PlaceOrder(Order order)
    {
        // Included via class attribute
    }

    [HttpHandler("/orders/{id}")]  // Additional attribute
    public Order GetOrder(int orderId)
    {
        // Included via class attribute (HttpHandler is ignored if not in FacadeOf)
    }
}
```

## Attribute Inheritance

Terminus supports attribute inheritance. Derived attributes are recognized when matching methods to facades:

```csharp
// Base attribute
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class HandlerAttribute : Attribute
{
}

// Derived attribute
public class CommandHandlerAttribute : HandlerAttribute
{
}

// Facade looking for HandlerAttribute
[FacadeOf<HandlerAttribute>]
public partial interface IAppFacade
{
}

// Both attributes match!
public class MyService
{
    [Handler]
    public void Method1() { }  // Included

    [CommandHandler]
    public void Method2() { }  // Also included (inherits from HandlerAttribute)
}
```

## Multiple Attributes

### On the Facade

A facade can aggregate methods from multiple attribute types:

```csharp
[FacadeOf<CommandAttribute, QueryAttribute>]
public partial interface IAppFacade
{
}

public class MyService
{
    [Command]
    public void Execute() { }  // Included

    [Query]
    public string GetData() { }  // Included

    [Event]
    public void Notify() { }  // NOT included (Event not specified in FacadeOf)
}
```

### On Methods

A single method can have multiple attributes, but it will only appear **once** in the facade:

```csharp
[FacadeOf<CommandAttribute, QueryAttribute>]
public partial interface IAppFacade
{
}

public class MyService
{
    [Command]
    [Query]
    public void ProcessData() { }  // Appears only once in IAppFacade
}
```

## Attribute Design Guidelines

### 1. Use Descriptive Names

Choose names that clearly indicate the purpose of the attribute:

```csharp
// Good
public class HttpHandlerAttribute : Attribute { }
public class CommandAttribute : Attribute { }
public class QueryAttribute : Attribute { }

// Less clear
public class MyAttribute : Attribute { }
public class ActionAttribute : Attribute { }
```

### 2. Apply `AttributeUsage` Correctly

Always specify where your attribute can be applied:

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class HandlerAttribute : Attribute
{
}
```

Common targets:
- `AttributeTargets.Method` - Method-level only
- `AttributeTargets.Class` - Class-level only
- `AttributeTargets.Method | AttributeTargets.Class` - Both (recommended)

### 3. Add `AllowMultiple` if Needed

If your attribute should be applied multiple times to the same method:

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RouteAttribute : Attribute
{
    public string Path { get; }
    public RouteAttribute(string path) => Path = path;
}

// Usage:
[Route("/users/{id}")]
[Route("/members/{id}")]
public User GetUser(int id) { ... }
```

### 4. Keep Attributes Lightweight

Attributes should be simple markers or metadata containers:

```csharp
// Good - Simple metadata
public class HttpHandlerAttribute : Attribute
{
    public string Route { get; }
    public string Method { get; set; } = "GET";

    public HttpHandlerAttribute(string route) => Route = route;
}

// Bad - Complex logic in attribute (move to separate service)
public class HttpHandlerAttribute : Attribute
{
    public void ProcessRequest() { /* complex logic */ }
}
```

## Attribute Scope and Visibility

### Public Attributes

Most attributes should be public so they can be used across assemblies:

```csharp
public class HandlerAttribute : Attribute { }
```

### Internal Attributes

Use `internal` for attributes specific to a single assembly:

```csharp
internal class HandlerAttribute : Attribute { }
```

> **Note:** Internal attributes work fine with Terminus, but facade interfaces and implementation classes must be in the same assembly.

## Common Attribute Patterns

### 1. Command-Query Separation (CQS)

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class CommandAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class QueryAttribute : Attribute { }

[FacadeOf<CommandAttribute>(CommandName = "Execute")]
public partial interface ICommands { }

[FacadeOf<QueryAttribute>(QueryName = "Query")]
public partial interface IQueries { }
```

### 2. Domain-Specific Markers

```csharp
public class InventoryOperationAttribute : Attribute { }
public class ShippingOperationAttribute : Attribute { }

[FacadeOf<InventoryOperationAttribute, ShippingOperationAttribute>]
public partial interface ILogisticsFacade { }
```

### 3. HTTP-Style Routing

```csharp
public class HttpHandlerAttribute : Attribute
{
    public string Route { get; }
    public HttpHandlerAttribute(string route) => Route = route;
}

[FacadeOf<HttpHandlerAttribute>]
public partial interface IHttpHandlers { }

public class UserService
{
    [HttpHandler("/users/{id}")]
    public User GetUser(int id) { ... }

    [HttpHandler("/users")]
    public void CreateUser(User user) { ... }
}
```

## Next Steps

- Understand [Service Resolution](service-resolution.md) strategies for method invocation
- Explore [Cross-Assembly Discovery](cross-assembly-discovery.md) for multi-project facades
- Learn about [Async Support](async-support.md) for asynchronous methods
- Explore [Advanced Scenarios](../guides/advanced-scenarios.md) including custom method naming
