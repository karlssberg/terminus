# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Essential Commands

### Building
```bash
# Build the entire solution
dotnet build

# Build a specific project
dotnet build src/Terminus.Generator/Terminus.Generator.csproj

# Build in Release mode
dotnet build -c Release
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test -v detailed

# Run a specific test
dotnet test --filter "FullyQualifiedName~EntryPointDiscoveryGeneratorTests"

# Run tests for a specific project
dotnet test test/Terminus.Generator.Tests.Unit/Terminus.Generator.Tests.Unit.csproj
```

### Running Examples
```bash
# Run the HelloWorld example
dotnet run --project examples/Terminus.Generator.Examples.HelloWord/Terminus.Generator.Examples.HelloWord.csproj

# Run the Web example
dotnet run --project examples/Terminus.Generator.Examples.Web/Terminus.Generator.Examples.Web.csproj
```

### Debugging Generated Code
To view the actual generated source code:
1. Add `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` to the `.csproj` file
2. Build the project
3. Check `obj/Debug/netX.0/generated/` or look in `examples/*/Generated/` folders

## Project Overview

**Terminus** is a C# source generator framework that implements a compile-time mediator pattern for method discovery and invocation. It provides a type-safe, reflection-free way to discover and invoke methods marked with custom attributes at runtime.

**Primary Use Case: Framework & Library Authors**

Terminus is primarily designed for **library authors** building:
- **Event-Driven Architecture (EDA) protocols** - Message handlers, event processors, command dispatchers
- **IO endpoint frameworks** - HTTP endpoints, gRPC services, message queue consumers, WebSocket handlers
- **Custom routing systems** - Any system that needs to route external inputs to methods based on attributes

Library authors can create custom attributes (e.g., `[HttpPost("/users")]`, `[MessageHandler("OrderCreated")]`, `[EventListener("user.created")]`) and leverage Terminus to automatically discover and route to handler methods at compile-time.

### Key Features

- **Compile-time code generation**: Uses Roslyn source generators to discover and generate mediator code
- **Type-safe mediation**: Generates strongly-typed mediator interfaces for method invocation
- **Flexible parameter binding**: Extensible parameter resolution system with custom binding strategies
- **Attribute-based discovery**: Methods marked with `[EntryPoint]` (or derived attributes) are automatically discovered
- **Custom attribute support**: Create domain-specific attributes inheriting from `EntryPointAttribute` for your protocol/framework
- **Runtime routing**: `IEntryPointRouter` enables dynamic routing based on external data (HTTP routes, message types, etc.)
- **Dependency injection integration**: Seamless integration with Microsoft.Extensions.DependencyInjection
- **Multi-framework support**: Targets .NET 7.0+, with specific support for net472 in tests

### Core Value Proposition

Traditional mediator patterns rely on runtime reflection or manual registration. Terminus moves this work to compile-time, generating:
1. Mediator interfaces with strongly-typed method signatures
2. Service registration code that wires up entry points
3. Invocation logic with parameter resolution and scope management

This eliminates runtime overhead, provides compile-time safety, and enables rich tooling support.

**For library authors**: Terminus provides the infrastructure to build attribute-based routing frameworks without implementing reflection-based discovery or manual registration systems. Your users mark methods with your custom attributes, and Terminus handles the discovery, registration, and invocation plumbing.

## Solution Structure

The solution contains 6 projects organized as follows:

### Core Projects

**Terminus.Attributes** (`src/Terminus.Attributes/`)
- Targets: `netstandard2.0` (maximum compatibility)
- Contains attribute definitions used by both generator and runtime
- Key files:
  - `EntryPointAttribute.cs` - Base attribute for marking entry point methods
  - `EntryPointAutoGenerateAttribute.cs` - Marks interfaces to generate mediator implementations
  - `ParameterBinderAttribute.cs` - Base for custom parameter binding attributes
- **Important**: Referenced by both the generator and consumer projects

**Terminus.Generator** (`src/Terminus.Generator/`)
- Targets: `netstandard2.0` (Roslyn requirement)
- The Roslyn source generator that runs at compile-time
- Key files:
  - `EntryPointDiscoveryGenerator.cs` - Main generator implementing `IIncrementalGenerator`
  - `EntrypointRegistrationSourceBuilder.cs` - Generates source code using Roslyn APIs
  - `EntryPointMethodInfo.cs` & `MediatorInterfaceInfo.cs` - Models for discovered items
- **Important**: Runs in the compiler process, not in the consuming application
- Uses `<IsRoslynComponent>true</IsRoslynComponent>` to indicate it's an analyzer

**Terminus** (`src/Terminus/`)
- Targets: `netstandard2.0` (runtime library)
- Runtime components used by generated code and consuming applications
- Key components:
  - `EntryPointDescriptor.cs` - Runtime descriptor wrapping entry point methods
  - `ParameterBindingStrategyResolver.cs` - Resolves parameters using strategy chain
  - `Dispatcher.cs` & `ScopedDispatcher.cs` - Runtime invocation infrastructure
  - `IEntryPointRouter.cs` & `DefaultEntryPointRouter.cs` - Entry point selection logic
  - `Strategies/` - Built-in parameter binding strategies
- **Important**: This is what gets deployed with your application

### Example Projects

**Terminus.Generator.Examples.HelloWord** (`examples/Terminus.Generator.Examples.HelloWord/`)
- Simple console application demonstrating the **Mediator Pattern**
- Shows both synchronous and async entry points
- Uses `[EntryPointMediator]` to generate strongly-typed mediator interface
- **Use case**: Application developers using Terminus directly

**Terminus.Generator.Examples.Web** (`examples/Terminus.Generator.Examples.Web/`)
- ASP.NET Core web application demonstrating the **Dispatcher Pattern for Library Authors**
- Shows custom attribute types (`MyHttpPostAttribute` with route information)
- Demonstrates `IAsyncDispatcher` with custom `IEntryPointRouter` implementation
- **Use case**: Library authors building HTTP/REST frameworks with attribute-based routing
- **Key files**:
  - `CustomRouter.cs` - Example of `IEntryPointRouter<T>` for path-based routing
  - `RouteEntry.cs` & `RouteMatch.cs` - Route matching infrastructure
  - `Program.cs` - Shows how a framework would integrate Terminus

### Test Projects

**Terminus.Generator.Tests.Unit** (`test/Terminus.Generator.Tests.Unit/`)
- Targets: `net8.0;net9.0;net472` (multi-framework testing)
- Uses Roslyn testing infrastructure: `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit`
- Key infrastructure:
  - `TerminusSourceGeneratorTest.cs` - Base class providing test harness
- Tests both positive cases and edge cases for code generation

### Project Dependency Graph

```
Terminus.Attributes (no dependencies)
       ↑
       ├─── Terminus.Generator → produces code for → Consumer Projects
       │
       └─── Terminus (runtime)
                ↑
                └─── Consumer Projects (examples, tests)
```

**Key architectural points:**
- `Terminus.Attributes` is referenced by everyone (generator, runtime, consumers)
- `Terminus.Generator` analyzes consumer code at compile-time
- Generated code depends on `Terminus` (runtime) for execution
- Consumers only deploy `Terminus.Attributes` and `Terminus` - the generator runs only at build time

## Key Concepts

### Entry Points

Methods marked with `[EntryPoint]` are discovered at compile time:

```csharp
public class MyHandlers
{
    [EntryPoint]
    public void Handle(string message)
    {
        Console.WriteLine(message);
    }
}
```

The generator discovers these methods and creates registration code.

### Entry Point Mediators

Interfaces marked with `[EntryPointMediator]` get generated implementations:

```csharp
[EntryPointMediator]
public partial interface IMediator;
```

The generator creates:
1. A partial interface definition with all discovered entry point method signatures
2. A concrete mediator class that implements the interface
3. Service registration extensions

### Generated Code Structure

For each discovered entry point, the generator produces:

**1. Mediator Interface Methods**
```csharp
public partial interface IMediator
{
    void Handle(string message);
}
```

**2. Mediator Implementation**
```csharp
internal sealed class IMediator_Generated : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ParameterBindingStrategyResolver _resolver;

    public void Handle(string message)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            // Resolve service and invoke method
            scope.ServiceProvider.GetRequiredService<MyHandlers>()
                .Handle(message);
        }
    }
}
```

**3. Service Registration**
```csharp
public static IServiceCollection AddEntryPoints(this IServiceCollection services)
{
    // Registers EntryPointDescriptor<T> for each discovered method
    // Registers mediator implementations
}
```

### Parameter Binding

Parameters are resolved using a strategy chain. Built-in strategies:

1. **ParameterNameBindingStrategy**: Resolves from context data by parameter name
2. **CancellationTokenBindingStrategy**: Provides CancellationToken from context
3. **DependencyInjectionBindingStrategy**: Resolves from IServiceProvider (fallback)

Custom strategies can be added:

```csharp
builder.Services.AddEntryPoints(resolver =>
{
    resolver.AddStrategy(new MyCustomBindingStrategy());
    resolver.RegisterParameterBinder<MyAttribute>(new MyBinder());
});
```

### Custom Entry Point Attributes

**Essential for Library Authors**: Create domain-specific attributes for your framework by inheriting from `EntryPointAttribute`.
**Alternative to Mediatr library**: Create strongly typed mediators, so that missing handlers are resolved at design/compile time. 

**Simple Custom Attribute:**
```csharp
// Your library defines this attribute
public class CommandAttribute : EntryPointAttribute { }

// Optionally create a mediator interface for it
[EntryPointMediator(typeof(CommandAttribute))]
public partial interface ICommandMediator;

// Your users mark their methods
public class MyCommands
{
    [Command]
    public void Execute() { }
}
```

**Attribute with Metadata (Recommended for Routing):**
```csharp
// HTTP endpoint attribute with route information
[AttributeUsage(AttributeTargets.Method)]
public class HttpGetAttribute : EntryPointAttribute
{
    public string Route { get; }

    public HttpGetAttribute(string route)
    {
        Route = route;
    }
}

// Message handler attribute with message type
[AttributeUsage(AttributeTargets.Method)]
public class MessageHandlerAttribute : EntryPointAttribute
{
    public string MessageType { get; }

    public MessageHandlerAttribute(string messageType)
    {
        MessageType = messageType;
    }
}

// Event listener with event name
[AttributeUsage(AttributeTargets.Method)]
public class EventListenerAttribute : EntryPointAttribute
{
    public string EventName { get; }
    public int Priority { get; set; } = 0;

    public EventListenerAttribute(string eventName)
    {
        EventName = eventName;
    }
}
```

**Usage by your library users:**
```csharp
public class Handlers
{
    [HttpGet("/users/{id}")]
    public User GetUser(string id) { /* ... */ }

    [MessageHandler("OrderCreated")]
    public void OnOrderCreated(OrderCreatedMessage msg) { /* ... */ }

    [EventListener("user.registered", Priority = 10)]
    public async Task OnUserRegistered(UserRegisteredEvent evt) { /* ... */ }
}
```

**Key points for library authors:**
- The generator discovers methods with derived attributes and groups them by exact attribute type
- Each attribute type gets its own `AddEntryPointsFor_{AttributeType}()` extension method
- Use attribute properties (like `Route`, `MessageType`, `EventName`) in your `IEntryPointRouter` implementation to perform routing
- Users of your library don't need to know about Terminus - they just apply your attributes

### Type Resolution

- **Static methods**: Invoked directly on the type
- **Instance methods**: Type is resolved from DI container per invocation
- **Scoped invocation**: Each mediator method call creates a new DI scope

### Invocation Patterns

Terminus supports two patterns for invoking entry points:

**1. Mediator Pattern (Compile-time Type Safety)**

Best for: Application developers using Terminus directly in their applications.

```csharp
// Define a mediator interface with [EntryPointMediator]
[EntryPointMediator]
public partial interface IMediator;

// Use strongly-typed methods
var mediator = serviceProvider.GetRequiredService<IMediator>();
mediator.Handle("hello"); // Compile-time checked
var result = await mediator.Query("foo", "bar", cancellationToken);
```

**2. Dispatcher Pattern (Runtime Routing) - PRIMARY PATTERN FOR LIBRARY AUTHORS**

Best for: Library/framework authors building EDA protocols, IO endpoints, or custom routing systems.

```csharp
// Use IDispatcher or IAsyncDispatcher for dynamic invocation
var dispatcher = serviceProvider.GetRequiredService<IAsyncDispatcher<YourCustomAttribute>>();

// Build context with external data (HTTP request, message envelope, etc.)
var context = new ParameterBindingContext(serviceProvider, dataDictionary);

// Fire-and-forget (void methods)
await dispatcher.PublishAsync(context, cancellationToken);

// Request-response (methods with return values)
var result = await dispatcher.RequestAsync<string>(context, cancellationToken);
```

**The dispatcher pattern is essential for library authors** building:
- **HTTP/REST frameworks**: Route HTTP requests to methods marked with `[HttpGet("/path")]`
- **Message queue consumers**: Dispatch messages to handlers based on `[MessageHandler("MessageType")]`
- **Event-driven systems**: Route events to listeners marked with `[EventListener("event.name")]`
- **gRPC/RPC frameworks**: Map RPC calls to service methods
- **WebSocket handlers**: Route WebSocket messages to appropriate handlers
- **Custom protocols**: Any system where external inputs determine which method to invoke

**Key advantage**: Your library users write simple attributed methods, and your framework handles routing without them needing to know about Terminus internals.

### Entry Point Routing

**Critical for Library Authors**: The `IEntryPointRouter<TAttribute>` system is how you implement custom routing logic for your framework.

```csharp
public interface IEntryPointRouter<TAttribute> where TAttribute : EntryPointAttribute
{
    EntryPointDescriptor<TAttribute> GetEntryPoint(ParameterBindingContext context);
}
```

**Routing Strategies:**

- **DefaultEntryPointRouter**: Selects entry points based on `MethodInfo` matching
- **Custom routers**: Implement `IEntryPointRouter<TAttribute>` to add custom routing logic

**Example: HTTP Routing**
```csharp
public class HttpRouter : IEntryPointRouter<MyHttpPostAttribute>
{
    private readonly Dictionary<string, EntryPointDescriptor<MyHttpPostAttribute>> _routes;

    public HttpRouter(IEnumerable<EntryPointDescriptor<MyHttpPostAttribute>> descriptors)
    {
        // Build route table from attribute properties
        _routes = descriptors
            .SelectMany(d => d.Attributes.Select(attr => (attr.Path, Descriptor: d)))
            .ToDictionary(x => x.Path, x => x.Descriptor);
    }

    public EntryPointDescriptor<MyHttpPostAttribute> GetEntryPoint(ParameterBindingContext context)
    {
        var path = context.GetData<string>("path");
        return _routes[path]; // Match incoming HTTP path to handler
    }
}
```

**Example: Message Type Routing**
```csharp
public class MessageRouter : IEntryPointRouter<MessageHandlerAttribute>
{
    public EntryPointDescriptor<MessageHandlerAttribute> GetEntryPoint(ParameterBindingContext context)
    {
        var messageType = context.GetData<string>("messageType");
        // Find handler where attribute.MessageType matches incoming message
        return _descriptors.First(d =>
            d.Attributes.Any(attr => attr.MessageType == messageType));
    }
}
```

See `examples/Terminus.Generator.Examples.Web/CustomRouter.cs` for a complete HTTP routing implementation.

## Guide for Library Authors

This section provides a comprehensive guide for library authors building EDA protocols, IO endpoint frameworks, or custom routing systems using Terminus.

### Building a Custom Framework with Terminus

**Step 1: Define Your Custom Attribute**

Create an attribute that inherits from `EntryPointAttribute` with properties relevant to your routing logic:

```csharp
// In your library: MyFramework.Core
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class MessageHandlerAttribute : EntryPointAttribute
{
    public string MessageType { get; }
    public int Priority { get; set; } = 0;

    public MessageHandlerAttribute(string messageType)
    {
        MessageType = messageType;
    }
}
```

**Step 2: Implement Custom Router**

Create a router that uses your attribute's properties to select the correct handler:

```csharp
public class MessageTypeRouter : IEntryPointRouter<MessageHandlerAttribute>
{
    private readonly ILookup<string, EntryPointDescriptor<MessageHandlerAttribute>> _handlersByMessageType;

    public MessageTypeRouter(IEnumerable<EntryPointDescriptor<MessageHandlerAttribute>> descriptors)
    {
        _handlersByMessageType = descriptors
            .SelectMany(d => d.Attributes.Select(attr => (MessageType: attr.MessageType, Descriptor: d)))
            .ToLookup(x => x.MessageType, x => x.Descriptor);
    }

    public EntryPointDescriptor<MessageHandlerAttribute> GetEntryPoint(ParameterBindingContext context)
    {
        var messageType = context.GetData<string>("messageType")
            ?? throw new InvalidOperationException("Message type not found in context");

        var handlers = _handlersByMessageType[messageType].ToList();

        if (!handlers.Any())
            throw new InvalidOperationException($"No handler found for message type: {messageType}");

        // Sort by priority if multiple handlers
        return handlers
            .OrderByDescending(h => h.Attributes.Max(a => a.Priority))
            .First();
    }
}
```

**Step 3: Create Framework Integration**

Build the public API for your framework:

```csharp
// In your library: MyFramework.Core
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyFramework(
        this IServiceCollection services,
        Action<MyFrameworkOptions>? configure = null)
    {
        var options = new MyFrameworkOptions();
        configure?.Invoke(options);

        // Register Terminus entry points
        services.AddEntryPoints<MessageHandlerAttribute>(resolver =>
        {
            // Configure parameter binding for your framework
            resolver.AddStrategy(new MessageBodyBindingStrategy());
            resolver.AddStrategy(new MessageMetadataBindingStrategy());
        });

        // Register your custom router
        services.AddSingleton<IEntryPointRouter<MessageHandlerAttribute>, MessageTypeRouter>();

        return services;
    }
}
```

**Step 4: Implement Framework Runtime**

Create the runtime component that receives external inputs and dispatches to handlers:

```csharp
public class MessageProcessor : IHostedService
{
    private readonly IAsyncDispatcher<MessageHandlerAttribute> _dispatcher;
    private readonly IServiceProvider _serviceProvider;

    public MessageProcessor(
        IAsyncDispatcher<MessageHandlerAttribute> dispatcher,
        IServiceProvider serviceProvider)
    {
        _dispatcher = dispatcher;
        _serviceProvider = serviceProvider;
    }

    public async Task ProcessMessage(IncomingMessage message, CancellationToken ct)
    {
        // Build context from incoming message
        var contextData = new Dictionary<string, object?>
        {
            ["messageType"] = message.Type,
            ["messageBody"] = message.Body,
            ["metadata"] = message.Metadata
        };

        var context = new ParameterBindingContext(_serviceProvider, contextData);

        // Dispatch to appropriate handler
        await _dispatcher.PublishAsync(context, ct);
    }

    // IHostedService implementation...
}
```

**Step 5: Optional - Custom Parameter Binding**

Create custom parameter binding strategies for domain-specific parameters:

```csharp
public class MessageBodyBindingStrategy : IParameterBindingStrategy
{
    public bool CanBind(ParameterBindingContext context)
    {
        // Bind parameters that expect the message body
        return context.ParameterName == "message" ||
               context.ParameterName == "body" ||
               context.ParameterType.Name.EndsWith("Message");
    }

    public object? Bind(ParameterBindingContext context)
    {
        var body = context.GetData<object>("messageBody");
        // Deserialize or convert to target parameter type
        return ConvertToType(body, context.ParameterType);
    }
}
```

### What Your Users See

With your framework built on Terminus, users have a clean, attribute-based API:

```csharp
// User application code
public class OrderHandlers
{
    private readonly IOrderRepository _repository;

    public OrderHandlers(IOrderRepository repository)
    {
        _repository = repository;
    }

    [MessageHandler("OrderCreated", Priority = 10)]
    public async Task HandleOrderCreated(OrderCreatedMessage message, CancellationToken ct)
    {
        var order = await _repository.CreateOrder(message.OrderData, ct);
        // ... handler logic
    }

    [MessageHandler("OrderCancelled")]
    public void HandleOrderCancelled(OrderCancelledMessage message)
    {
        // ... handler logic
    }
}

// Program.cs
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMyFramework(); // Your framework
await builder.Build().RunAsync();
```

**Users never need to know about:**
- Terminus infrastructure
- `IDispatcher` or `EntryPointDescriptor`
- Service registration details
- Routing logic

They just apply your attributes and write handler methods!

### Benefits for Library Authors

1. **No reflection overhead**: All discovery happens at compile-time
2. **Type-safe**: Generated code is strongly-typed
3. **Minimal boilerplate**: Terminus handles registration and invocation plumbing
4. **Flexible routing**: Implement custom routing logic via `IEntryPointRouter`
5. **Extensible parameter binding**: Support domain-specific parameter types
6. **DI integration**: Seamless integration with Microsoft.Extensions.DependencyInjection
7. **User-friendly**: Consumers use simple attributes without knowing about internals

## Development Guidelines

### Generator Development

**Key Generator Pattern:**
```csharp
[Generator]
public class EndpointDiscoveryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var discoveredMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsCandidateMethod,  // Fast syntax check
                transform: GetMethodWithDerivedAttribute)  // Semantic analysis
            .Collect();

        context.RegisterSourceOutput(discoveredMethods, Execute);
    }
}
```

**Two-phase discovery:**
1. **Syntax predicate**: Fast filter (methods with attributes)
2. **Semantic transform**: Check if attribute derives from `EntryPointAttribute`

### Code Generation Patterns

Located in `EntrypointRegistrationSourceBuilder.cs`:

1. **Raw string interpolation** for large code blocks
2. **Roslyn SyntaxFactory** for complex expressions
3. **NormalizeWhitespace()** for consistent formatting

Example:
```csharp
var rawCode = $$"""
    namespace Terminus.Generated
    {
        {{CreateMediatorTypeDefinitions(entryPoints)}}
    }
    """;

return ParseCompilationUnit(rawCode).NormalizeWhitespace();
```

### Framework-Specific Code

Use conditional compilation for framework differences:

```csharp
#if NET7_0_OR_GREATER
    public required string ParameterName { get; init; }
#else
    public string ParameterName { get; }
#endif
```

Common symbols:
- `NET7_0_OR_GREATER` - C# 11 features (required members, generic attributes)
- `NET8_0_OR_GREATER` - C# 12 features (SetsRequiredMembers)
- Test projects support `net472` for backward compatibility testing

### Naming Conventions

- **Generated mediator classes**: `{InterfaceName}_Generated` (e.g., `IMediator_Generated`)
- **Generated files**:
  - Per attribute type: `{AttributeTypeName}_Generated.g.cs` (e.g., `Terminus_Attributes_EntryPointAttribute_Generated.g.cs`)
  - Service registration: `__EntryPointServiceRegistration_Generated.g.cs`
- **Namespace**:
  - Mediator implementations: Same namespace as the mediator interface
  - Service extensions: `Terminus.Generated`
- **Extension methods**:
  - `ServiceCollectionExtensions.AddEntryPoints<TAttribute>()` - Generic method that routes to specific implementations
  - `ServiceCollectionExtensions.AddEntryPointsFor_{AttributeTypeName}()` - Generated for each attribute type

### Testing Strategy

**Test Infrastructure:**

All generator tests inherit from `TerminusSourceGeneratorTest<T>`:

```csharp
var test = new TerminusSourceGeneratorTest<EndpointDiscoveryGenerator>
{
    TestState =
    {
        Sources = { inputSource }
    }
};

test.TestState.GeneratedSources.Add(
    (typeof(EndpointDiscoveryGenerator), "EntryPoints.g.cs",
     SourceText.From(expectedOutput, Encoding.UTF8)));

await test.RunAsync();
```

**Test harness provides:**
- `IsExternalInit` shim for record types on older frameworks
- Minimal DI shims to avoid heavy package references
- Reference to `Terminus.Attributes` assembly

**Testing approach:**
1. Write input source with `[EntryPoint]` methods
2. Write expected generated output
3. Use Roslyn testing infrastructure to verify match
4. Test both positive cases and edge cases

### Common Patterns

**1. Attribute inheritance checking:**
```csharp
private static bool InheritsFromBaseAttribute(INamedTypeSymbol attributeClass)
{
    var current = attributeClass;
    while (current != null)
    {
        if (current.ToDisplayString() == BaseAttributeFullName)
            return true;
        current = current.BaseType;
    }
    return false;
}
```

**2. Grouping by attribute type:**
```csharp
entryPoints
    .GroupBy(e => e.AttributeData.AttributeClass!, SymbolEqualityComparer.Default)
    .Select(group => GenerateForAttributeType(group))
```

**3. Service registration with if/else chain:**
Generates runtime type checks to register the correct entry points based on the generic `TAttribute` parameter.

**4. Parameter resolution:**
```csharp
resolver.ResolveParameter<TParameter>("paramName", context)
```
This is injected into generated code and resolved at runtime.

### Extension Points

**1. Custom parameter binding strategies:**
```csharp
public class MyStrategy : IParameterBindingStrategy
{
    public bool CanBind(ParameterBindingContext context) => /* check */;
    public object? Bind(ParameterBindingContext context) => /* resolve */;
}

services.AddEntryPoints(r => r.AddStrategy(new MyStrategy()));
```

**2. Custom parameter binders via attributes:**
```csharp
public class FromQueryAttribute : ParameterBinderAttribute
{
    public override Type BinderType => typeof(QueryStringBinder);
}

public class QueryStringBinder : IParameterBinder
{
    public object? BindParameter(ParameterBindingContext context)
    {
        return context.GetData<HttpContext>("httpContext")
            ?.Request.Query[context.ParameterName];
    }
}

services.AddEntryPoints(r =>
    r.RegisterParameterBinder<FromQueryAttribute>(new QueryStringBinder()));
```

**3. Custom entry point attributes:**
Create attributes inheriting from `EntryPointAttribute` for domain-specific semantics.

## Architecture Deep Dive

### Generator Pipeline

1. **Syntax filtering** (`IsCandidateMethod`):
   - Checks for `MethodDeclarationSyntax` with attributes
   - Fast, no semantic analysis

2. **Semantic transform** (`GetMethodWithDerivedAttribute`):
   - Gets `IMethodSymbol` from semantic model
   - Walks attribute inheritance chain
   - Creates `EntryPointMethodInfo` if matches

3. **Collection**:
   - Groups all discovered methods
   - Passed to `Execute` for code generation

4. **Code generation** (`Execute`):
   - Calls `EntrypointRegistrationSourceBuilder.Generate()` for each attribute type
   - Produces one file per attribute type: `{AttributeType}_Generated.g.cs`
   - Produces one shared service registration file: `__EntryPointServiceRegistration_Generated.g.cs`

### Parameter Resolution Flow

1. Method invocation requested via mediator
2. For each parameter, call `resolver.ResolveParameter<T>(name, context)`
3. Resolver checks for custom binder attribute
4. If not, iterates through strategy chain
5. First strategy where `CanBind` returns true is used
6. Falls back to DI resolution if no strategy matches

### Scope Management

Each mediator method invocation:
1. Creates a new DI scope via `_serviceProvider.CreateScope()`
2. Resolves service instance within scope
3. Invokes method with resolved parameters
4. Disposes scope automatically

This ensures proper lifetime management for scoped services.

### Type-Safe Mediator Pattern

Traditional mediator patterns use:
```csharp
await mediator.Send(new MyCommand()); // Stringly-typed or marker interfaces
```

Terminus generates:
```csharp
mediator.Handle(message); // Compile-time type checking
```

Benefits:
- Compile-time safety
- Refactoring support
- IntelliSense/tooling
- No reflection at runtime

## Common Development Scenarios

### Adding a new binding strategy

1. Implement `IParameterBindingStrategy`
2. Add to `DefaultParameterBindingStrategies.Create()` or register via API
3. Update tests to cover new strategy
4. Strategies are evaluated in order - earlier strategies have priority

### Modifying generated code

1. Update `EntrypointRegistrationSourceBuilder.cs`
2. Update expected outputs in `EndpointDiscoveryGeneratorTests.cs`
3. Run tests to verify generation correctness
4. Consider backward compatibility

### Supporting a new attribute pattern

1. Create new attribute in `Terminus.Attributes`
2. Generator automatically discovers derived attributes
3. Optionally enhance `EntryPointAutoGenerateAttribute` to specify which attribute type to mediate
4. Add tests covering the new pattern

### Debugging generator issues

1. Set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` in consumer project
2. Check `obj/generated/` folder for actual generated code
3. Add breakpoint in generator code (requires special setup)
4. Use `Debugger.Launch()` in generator for JIT debugging

## Project Conventions

### File Organization

- Attributes in `Terminus.Attributes` project (referenced by both generator and runtime)
- Generator logic in `Terminus.Generator` project
- Runtime components in `Terminus` project
- Each strategy/binder in its own file
- Test file names match implementation: `XGenerator.cs` → `XGeneratorTests.cs`

### Code Style

- Use C# 12 features where appropriate (file-scoped namespaces, raw strings, collection expressions)
- Use `readonly` for fields that don't change
- Prefer expression-bodied members for simple methods
- Use `var` for obvious types
- Use explicit types for clarity in complex scenarios

### Error Handling

- Generator should not throw exceptions (use diagnostics instead)
- Runtime binding failures throw descriptive exceptions
- Include parameter name and type in error messages
- Validate configuration at registration time when possible

## Testing Guidelines

### Generator Tests

**What to test:**
- Single method with parameters
- Multiple methods
- Static vs instance methods
- Methods with no parameters
- Methods with return values
- Custom attribute types
- Multiple attribute types in same compilation

**Test structure:**
```csharp
[Fact]
public async Task Given_X_Should_generate_Y()
{
    const string source = """...""";
    const string expected = """...""";

    var test = new TerminusSourceGeneratorTest<EndpointDiscoveryGenerator>
    {
        TestState = { Sources = { source } }
    };

    test.TestState.GeneratedSources.Add((generator, filename, expected));
    await test.RunAsync();
}
```

### Runtime Tests

Currently minimal. Consider adding:
- Parameter binding strategy tests
- Resolver configuration tests
- Integration tests with actual DI container
- End-to-end mediator invocation tests

## Known Patterns and Idioms

### Incremental Generator Pattern

Terminus uses `IIncrementalGenerator` (not older `ISourceGenerator`):
- Better performance with caching
- Handles partial changes efficiently
- Required for modern Roslyn generators

### Symbol Equality

Always use `SymbolEqualityComparer.Default` when comparing symbols:
```csharp
.GroupBy(e => e.AttributeData.AttributeClass!, SymbolEqualityComparer.Default)
```

### Type Name Serialization

Use `ToDisplayString()` to get fully-qualified type names:
```csharp
var typeName = methodSymbol.ContainingType.ToDisplayString();
// e.g., "MyNamespace.MyClass"
```

### Attribute Argument Access

```csharp
var typeArg = attributeData.ConstructorArguments
    .Select(arg => arg.Value)
    .OfType<Type>()
    .FirstOrDefault();
```

## Future Considerations

### Potential Enhancements

1. **Method overload support**: Currently assumes unique method names within the same attribute type
2. **Diagnostics**: Report errors/warnings during generation (missing services, ambiguous methods)
3. **Source link support**: Map generated code back to original methods
4. **Batch invocation**: Invoke multiple entry points in a transaction
5. **Interceptors**: Pre/post processing hooks for entry point invocation
6. **Async scope support**: Use `CreateAsyncScope()` for async entry points (infrastructure exists but not yet used)

### Performance Optimizations

- Cache `MethodInfo` lookups
- Avoid scope creation for static methods
- Support singleton-lifetime mediators
- Consider async method support

### API Surface

Current API is minimal. Consider:
- Builder pattern for more complex configuration
- Fluent API for strategy registration
- More granular control over code generation
- Support for custom mediator implementations

---

## Quick Reference

### Key Files to Understand

**For Generator Development:**
1. `EntryPointDiscoveryGenerator.cs` - Entry point for source generation
2. `EntrypointRegistrationSourceBuilder.cs` - Code generation logic
3. `EntryPointDescriptor.cs` - Runtime method descriptor
4. `TerminusSourceGeneratorTest.cs` - Test infrastructure

**For Library Authors:**
1. `examples/Terminus.Generator.Examples.Web/` - Complete example of library author pattern
2. `IEntryPointRouter.cs` - Interface for custom routing logic
3. `ParameterBindingStrategyResolver.cs` - Parameter resolution system
4. `IParameterBindingStrategy.cs` - Interface for custom parameter binding

### Common Tasks

**For Terminus Contributors:**

**Add a new strategy:**
→ Create class implementing `IParameterBindingStrategy` in `Strategies/`

**Modify generated code:**
→ Edit `EntrypointRegistrationSourceBuilder.cs`

**Test generator changes:**
→ Update expected strings in `EndpointDiscoveryGeneratorTests.cs`

**Debug generation:**
→ Set `EmitCompilerGeneratedFiles=true` and check `obj/generated/`

**For Library Authors Using Terminus:**

**Create custom attribute:**
→ Inherit from `EntryPointAttribute`, add properties for routing metadata

**Implement routing:**
→ Implement `IEntryPointRouter<YourAttribute>` to select handlers based on external data

**Add custom parameter binding:**
→ Implement `IParameterBindingStrategy` and register in your `AddEntryPoints` call

**See complete example:**
→ Study `examples/Terminus.Generator.Examples.Web/` for end-to-end implementation
- use context7
- DO NOT use StringBuilder to compose code snippets when generating source using Roslyn
- When using Roslyn, DO use SyntaxFactory for composition of highly dynamic code snippets
- When using Roslyn, DO use the SyntaxFactory parsers to create strongly type chunks of static or easily templated source snippets
- DO (when using Roslyn) try and define the composition root as as a templated string (for readability).  If templating beocmes excessive then use a SyntaxFactory
- When writing multiline strings in C#, use the raw string literal syntax (""").  Please keep the open and closing """ (and variants) vertically aligned by starting on a new line (particularly when doing assignments)