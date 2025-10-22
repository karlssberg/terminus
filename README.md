# Terminus

A lightweight .NET framework for creating generic server-side endpoints that can be wired up to any infrastructure. Terminus allows developers to use POCO (Plain Old CLR Objects) types as entrypoints to their services by decorating methods with custom attributes.

## Features

- **Infrastructure Agnostic**: Decouple your service entrypoints from specific infrastructure implementations
- **Attribute-Based**: Simple attribute decoration to mark methods as endpoints
- **Automatic Discovery**: Scan assemblies to automatically discover and register endpoints
- **Flexible Invocation**: Support for both synchronous and asynchronous endpoint methods
- **Tagging System**: Organize endpoints with tags for easy filtering and categorization
- **Type-Safe**: Strongly-typed endpoint metadata with full reflection support
- **Extensible**: Easy to extend for custom infrastructure integrations

## Installation

```bash
dotnet add package Terminus
```

Or add to your `.csproj` file:

```xml
<PackageReference Include="Terminus" Version="1.0.0" />
```

## Quick Start

### 1. Define an Endpoint

```csharp
using Terminus;

public class CalculatorEndpoint : IEndpoint
{
    [Endpoint]
    public int Add(int a, int b)
    {
        return a + b;
    }

    [Endpoint("Multiply")]
    public int MultiplyNumbers(int a, int b)
    {
        return a * b;
    }
}
```

### 2. Discover and Register Endpoints

```csharp
// Create a registry
var registry = new EndpointRegistry();

// Register endpoints from an assembly
registry.RegisterEndpoints(typeof(Program).Assembly);

// Or register from a specific type
registry.RegisterEndpoints(typeof(CalculatorEndpoint));
```

### 3. Invoke Endpoints

```csharp
var calculator = new CalculatorEndpoint();
var endpoint = registry.GetEndpoint("Add");

// Invoke the endpoint
var result = EndpointInvoker.Invoke(endpoint, calculator, 5, 3);
Console.WriteLine($"Result: {result}"); // Output: Result: 8
```

## Usage Examples

### Async Endpoints

```csharp
public class DataServiceEndpoint : IEndpoint
{
    [Endpoint]
    public async Task<string> FetchDataAsync(string url)
    {
        var client = new HttpClient();
        return await client.GetStringAsync(url);
    }
}

// Invoke async endpoint
var service = new DataServiceEndpoint();
var endpoint = registry.GetEndpoint("FetchDataAsync");
var result = await EndpointInvoker.InvokeAsync(endpoint, service, "https://api.example.com");
```

### Tagged Endpoints

```csharp
public class UserServiceEndpoint : IEndpoint
{
    [Endpoint(Tags = new[] { "user", "query" })]
    public string GetUser(int userId)
    {
        return $"User {userId}";
    }

    [Endpoint(Tags = new[] { "user", "mutation" })]
    public string CreateUser(string name, string email)
    {
        return $"Created: {name}";
    }
}

// Query endpoints by tag
var queryEndpoints = registry.GetEndpointsByTag("query");
var mutationEndpoints = registry.GetEndpointsByTag("mutation");
```

### Custom Endpoint Names

```csharp
public class MyEndpoint : IEndpoint
{
    // Endpoint name will be "Add" (method name)
    [Endpoint]
    public int Add(int a, int b) => a + b;

    // Endpoint name will be "CustomName" (specified in attribute)
    [Endpoint("CustomName")]
    public int Multiply(int a, int b) => a * b;
}
```

## Core Components

### IEndpoint Interface

Marker interface that identifies a class as containing endpoint methods.

```csharp
public interface IEndpoint { }
```

### EndpointAttribute

Attribute to mark methods as endpoints.

```csharp
[Endpoint]                                    // Uses method name
[Endpoint("CustomName")]                      // Custom name
[Endpoint(Tags = new[] { "tag1", "tag2" })]  // With tags
```

### EndpointRegistry

Central registry for managing discovered endpoints.

```csharp
var registry = new EndpointRegistry();
registry.RegisterEndpoints(assembly);          // Register from assembly
registry.RegisterEndpoints(typeof(MyEndpoint)); // Register from type
registry.GetEndpoint("EndpointName");          // Get by name
registry.GetEndpointsByTag("tag");             // Get by tag
registry.Endpoints                             // Get all endpoints
```

### EndpointDiscovery

Static class for discovering endpoints.

```csharp
var endpoints = EndpointDiscovery.DiscoverEndpoints(assembly);
var endpoints = EndpointDiscovery.DiscoverEndpoints(typeof(MyEndpoint));
var endpoints = EndpointDiscovery.DiscoverEndpoints(); // Calling assembly
```

### EndpointInvoker

Utility for invoking endpoint methods.

```csharp
var result = EndpointInvoker.Invoke(metadata, instance, param1, param2);
var result = await EndpointInvoker.InvokeAsync(metadata, instance, param1, param2);
```

### EndpointMetadata

Contains metadata about a discovered endpoint.

```csharp
public class EndpointMetadata
{
    public Type EndpointType { get; }           // Type containing the endpoint
    public MethodInfo Method { get; }           // Endpoint method
    public string Name { get; }                 // Endpoint name
    public IReadOnlyList<string> Tags { get; }  // Associated tags
    public EndpointAttribute Attribute { get; } // Endpoint attribute
}
```

## Use Cases

### 1. Library Authors

Create reusable endpoint libraries that can be consumed by any infrastructure:

```csharp
// Your library
public class PaymentEndpoints : IEndpoint
{
    [Endpoint]
    public PaymentResult ProcessPayment(PaymentRequest request) { ... }
    
    [Endpoint]
    public RefundResult ProcessRefund(RefundRequest request) { ... }
}

// Users can wire this to REST APIs, gRPC, message queues, etc.
```

### 2. Service Decoupling

Decouple business logic from infrastructure concerns:

```csharp
// Business logic - infrastructure agnostic
public class OrderService : IEndpoint
{
    [Endpoint]
    public Order CreateOrder(CreateOrderRequest request) { ... }
}

// Infrastructure layer wires endpoints to HTTP, gRPC, messaging, etc.
```

### 3. Multi-Protocol Services

Expose the same endpoints through multiple protocols:

```csharp
// Define once
public class NotificationService : IEndpoint
{
    [Endpoint]
    public void SendNotification(string message) { ... }
}

// Wire to multiple infrastructures:
// - REST API controller
// - gRPC service
// - Message queue consumer
// - WebSocket handler
```

## Building from Source

```bash
git clone https://github.com/karlssberg/terminus.git
cd terminus
dotnet build
dotnet test
```

## Running the Sample

```bash
cd samples/Terminus.Sample
dotnet run
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Requirements

- .NET 8.0 or higher

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/karlssberg/terminus).