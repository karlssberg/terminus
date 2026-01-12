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

# Run a specific test class
dotnet test --filter "FullyQualifiedName~ClassName"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"

# Run tests for a specific project
dotnet test <path-to-test-project.csproj>

# Run tests without rebuilding (faster iteration)
dotnet test --no-build
```

### Debugging Generated Code
To view the actual generated source code:
1. Add `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` to the `.csproj` file
2. Build the project
3. Check `obj/Debug/netX.0/generated/` folder for generated files

## Project Overview

**Terminus** is a C# source generator framework that generates **facade implementations** at compile-time. It discovers methods marked with custom attributes and generates strongly-typed facade interfaces that delegate to those methods.

**Primary Use Case: Type-Safe Abstraction Layer**

Terminus enables developers to create clean, type-safe facades over implementation methods by:
- Marking an interface with `[FacadeOf(typeof(YourCustomAttribute))]`
- Marking implementation methods with your custom attribute
- Generating a facade implementation that delegates calls to those methods

### Key Features

- **Compile-time code generation**: Uses Roslyn source generators to discover and generate facade code
- **Type-safe facades**: Generates strongly-typed facade interfaces with explicit implementation
- **Flexible service resolution**: Supports static methods, scoped instances, and non-scoped instances
- **Custom attribute support**: Works with any custom attribute type you define
- **Scope management**: Automatic scope creation and disposal for scoped facades
- **Async support**: Full support for async methods, including `Task`, `ValueTask`, `Task<T>`, `ValueTask<T>`, and `IAsyncEnumerable<T>`
- **Generic method support**: Full support for generic methods with type parameters and constraints across all return types
- **Custom method naming**: Configure different method names based on return types (Command, Query, etc.)
- **Method aggregation**: Automatically aggregates methods with identical signatures into a single facade method, enabling notification/broadcast patterns (similar to MediatR)
- **Dependency injection integration**: Seamless integration with Microsoft.Extensions.DependencyInjection

### Core Value Proposition

Traditional facade patterns require manual implementation of delegate methods. Terminus moves this work to compile-time, generating:
1. Partial interface definition with all discovered method signatures
2. Sealed implementation class with explicit interface implementation
3. Service resolution logic based on method characteristics (static vs instance)
4. Scope management for scoped facades with proper disposal

This eliminates boilerplate, provides compile-time safety, and enables rich tooling support.

## Solution Structure

The solution contains 3 projects:

### Core Projects

**Terminus** (`src/Terminus/`)
- Targets: `net8.0;netstandard2.0;net10.0`
- Contains attribute definitions used by both generator and consumers
- Key files:
  - `FacadeOfAttribute.cs` - Marks interfaces to generate facade implementations
  - `FacadeImplementationAttribute.cs` - Applied to generated implementation classes
- **Important**: Referenced by consumer projects at runtime

**Terminus.Generator** (`src/Terminus.Generator/`)
- Targets: `netstandard2.0` (Roslyn requirement)
- The Roslyn source generator that runs at compile-time
- Key files:
  - `FacadeGenerator.cs` - Main generator implementing `IIncrementalGenerator`
  - `Discovery/` - Discovery logic for facades and methods
  - `Matching/` - Logic to match methods to facades
  - `Pipeline/FacadeGenerationPipeline.cs` - Orchestrates the generation process
  - `Builders/` - Modular builders for generating code using Roslyn
  - `UsageValidator.cs` - Validates discovered methods for errors
  - `Diagnostics.cs` - Diagnostic definitions
- **Important**: Runs in the compiler process, not in the consuming application
- Uses `<IsRoslynComponent>true</IsRoslynComponent>` to indicate it's an analyzer

### Test Projects

**Terminus.Generator.Tests.Unit** (`test/Terminus.Generator.Tests.Unit/`)
- Targets: `net8.0;net9.0;net472` (multi-framework testing)
- Uses Roslyn testing infrastructure: `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit`
- Key infrastructure:
  - `TerminusSourceGeneratorTest.cs` - Base class providing test harness
- Tests both positive cases and edge cases for code generation

### Project Dependency Graph

```
Terminus (attributes + runtime)
       ↑
       ├─── Terminus.Generator → produces code for → Consumer Projects
       │
       └─── Consumer Projects (tests, examples)
```

**Key architectural points:**
- `Terminus` contains attributes and is referenced by consumers at runtime
- `Terminus.Generator` analyzes consumer code at compile-time
- Generated code depends on `Terminus` for attributes
- Consumers only deploy `Terminus` - the generator runs only at build time

## Key Concepts

### Facade Pattern

Terminus implements the facade pattern through code generation. You define an interface, mark it with `[FacadeOf]`, and Terminus generates the implementation.

```csharp
// 1. Define your custom attribute
public class FacadeMethodAttribute : Attribute { }

// 2. Mark interface with [FacadeOf]
[FacadeOf(typeof(FacadeMethodAttribute))]
public partial interface IMyFacade;

// 3. Mark methods with your custom attribute
public class MyService
{
    [FacadeMethod]
    public void DoSomething(string input)
    {
        Console.WriteLine(input);
    }
}

// 4. Use the generated facade
var facade = new IMyFacade_Generated(serviceProvider);
facade.DoSomething("hello"); // Delegates to MyService.DoSomething
```

### FacadeOf Attribute

The `[FacadeOf]` attribute marks interfaces for facade generation:

```csharp
[AttributeUsage(AttributeTargets.Interface)]
public sealed class FacadeOfAttribute : Attribute
{
    // Constructor: specify one or more attribute types
    public FacadeOfAttribute(Type facadeMethodAttribute, params Type[] facadeMethodAttributes);

    // Whether to create scoped instances (default: false)
    public bool Scoped { get; set; }

    // Custom method names based on return types
    public string? CommandName { get; set; }        // void methods
    public string? QueryName { get; set; }          // Result methods
    public string? AsyncCommandName { get; set; }   // Task methods
    public string? AsyncQueryName { get; set; }     // Task<T> methods
    public string? AsyncStreamName { get; set; }    // IAsyncEnumerable<T> methods

    // Method aggregation control (default: None)
    public FacadeAggregationMode AggregationMode { get; set; }
}
```

**Example with custom naming:**
```csharp
[FacadeOf(typeof(HandlerAttribute),
    CommandName = "Execute",
    QueryName = "Query",
    AsyncCommandName = "ExecuteAsync",
    AsyncQueryName = "QueryAsync")]
public partial interface IHandlers;
```

### Custom Facade Method Attributes

Create domain-specific attributes for your methods. Attributes can be applied to individual methods or to the entire class to include all public methods:

```csharp
// Simple attribute
public class HandlerAttribute : Attribute { }

// Attribute with metadata
public class HttpHandlerAttribute : Attribute
{
    public string Route { get; }
    public HttpHandlerAttribute(string route) => Route = route;
}

// Method-level usage: Explicit per-method
public class MyHandlers
{
    [Handler]
    public void ProcessCommand(string command) { }

    [HttpHandler("/users/{id}")]
    public User GetUser(string id) { }
}

// Class-level usage: All public methods included automatically
[Handler]
public class MyHandlers
{
    public void ProcessCommand(string command) { }  // Included
    public void ExecuteAction(int id) { }           // Included
    public string QueryData() { }                   // Included

    private void InternalHelper() { }               // Excluded (not public)
}

// Mixed usage: Both class and method attributes work together
[Handler]
public class MyHandlers
{
    [Handler]  // Explicit method attribute (redundant but allowed)
    public void Method1() { }

    public void Method2() { }  // Also included via class attribute

    [HttpHandler("/special")]  // Different attribute on specific method
    public void Method3() { }  // Included via class attribute
}
```

**Class-level attribute behavior:**
- Applies to all public instance and static methods
- Excludes private, protected, and internal methods
- Excludes special methods (constructors, property accessors, operators, finalizers, etc.)
- Can be combined with method-level attributes (no duplicates in facade)
- Supports classes, structs, and records

### Generated Code Structure

For each facade interface, the generator produces:

**1. Partial Interface Definition**
```csharp
public partial interface IMyFacade
{
    void DoSomething(string input);
    Task<Result> GetDataAsync(int id);
}
```

**2. Sealed Implementation Class**
```csharp
[FacadeImplementation(typeof(global::MyNamespace.IMyFacade))]
public sealed class IMyFacade_Generated : global::MyNamespace.IMyFacade
{
    private readonly global::System.IServiceProvider _serviceProvider;

    public IMyFacade_Generated(global::System.IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    void global::MyNamespace.IMyFacade.DoSomething(string input)
    {
        // Service resolution and invocation
        global::Microsoft.Extensions.DependencyInjection
            .ServiceProviderServiceExtensions
            .GetRequiredService<MyService>(_serviceProvider)
            .DoSomething(input);
    }

    async global::System.Threading.Tasks.Task<Result> global::MyNamespace.IMyFacade.GetDataAsync(int id)
    {
        return await global::Microsoft.Extensions.DependencyInjection
            .ServiceProviderServiceExtensions
            .GetRequiredService<MyService>(_serviceProvider)
            .GetDataAsync(id)
            .ConfigureAwait(false);
    }
}
```

### Service Resolution Strategies

Terminus uses three strategies to resolve service instances:

**1. Static Service Resolution**
- Used for: Static methods
- Behavior: Direct invocation on the type (no service resolution)
- Example:
```csharp
public static class Utilities
{
    [Handler]
    public static void Log(string message) { }
}
// Generated: global::MyNamespace.Utilities.Log(message);
```

**2. Non-Scoped Service Resolution**
- Used for: Instance methods on non-scoped facades (`Scoped = false` or default)
- Behavior: Resolves service from root `IServiceProvider` per invocation
- Example:
```csharp
[FacadeOf(typeof(HandlerAttribute))]
public partial interface IHandlers;

public class MyService
{
    [Handler]
    public void DoWork() { }
}
// Generated: _serviceProvider.GetRequiredService<MyService>().DoWork();
```

**3. Scoped Service Resolution**
- Used for: Instance methods on scoped facades (`Scoped = true`)
- Behavior: Creates scope lazily, reuses for facade lifetime, disposes on disposal
- Example:
```csharp
[FacadeOf(typeof(HandlerAttribute), Scoped = true)]
public partial interface IHandlers;

public class MyService
{
    [Handler]
    public void DoWork() { }

    [Handler]
    public async Task DoWorkAsync() { }
}
// Generated:
// - Sync methods: _syncScope.Value.ServiceProvider.GetRequiredService<MyService>()
// - Async methods: _asyncScope.Value.ServiceProvider.GetRequiredService<MyService>()
```

**Scoped facades implement `IDisposable` and `IAsyncDisposable`:**
```csharp
public sealed class IHandlers_Generated : IHandlers, IDisposable, IAsyncDisposable
{
    private bool _syncDisposed;
    private bool _asyncDisposed;
    private readonly Lazy<IServiceScope> _syncScope;
    private readonly Lazy<AsyncServiceScope> _asyncScope;

    public void Dispose()
    {
        if (_syncDisposed || !_syncScope.IsValueCreated) return;
        _syncScope.Value.Dispose();
        _syncDisposed = true;
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_asyncDisposed || !_asyncScope.IsValueCreated) return;
        await _asyncScope.Value.DisposeAsync().ConfigureAwait(false);
        _asyncDisposed = true;
        GC.SuppressFinalize(this);
    }
}
```

### Return Type Detection

Terminus automatically detects return types and generates appropriate code:

| Return Type | ReturnTypeKind | Generated Code (Single Method) | Generated Code (Aggregated) | Async Modifier |
|-------------|---------------|--------------------------------|---------------------------|----------------|
| `void` | `Void` | `instance.Method();` | Execute all handlers | No |
| `T` | `Result` | `return instance.Method();` | `yield return` each result (returns `IEnumerable<T>`) | No |
| `Task` | `Task` | `await instance.Method().ConfigureAwait(false);` | Await all handlers | Yes |
| `Task<T>` | `TaskWithResult` | `return await instance.Method().ConfigureAwait(false);` | `yield return await` each result (returns `IAsyncEnumerable<T>`) | Yes |
| `ValueTask` | `Task` | `await instance.Method().ConfigureAwait(false);` | Await all handlers | Yes |
| `ValueTask<T>` | `TaskWithResult` | `return await instance.Method().ConfigureAwait(false);` | `yield return await` each result (returns `IAsyncEnumerable<T>`) | Yes |
| `IAsyncEnumerable<T>` | `AsyncEnumerable` | `await foreach (var item in ...) yield return item;` (scoped) or `return instance.Method();` (non-scoped) | Not aggregated | Yes (scoped) / No (non-scoped) |

**Note:** When multiple methods with the same signature are discovered, they are aggregated and return types are transformed as shown in the "Generated Code (Aggregated)" column. See [Method Aggregation](#method-aggregation) for details.

### Method Naming Strategy

By default, facade methods use the same name as the implementation method. You can customize names based on return types:

```csharp
[FacadeOf(typeof(HandlerAttribute),
    CommandName = "Execute",      // For void methods
    QueryName = "Query",          // For result methods
    AsyncCommandName = "ExecuteAsync",  // For Task methods
    AsyncQueryName = "QueryAsync",      // For Task<T> methods
    AsyncStreamName = "Stream")]        // For IAsyncEnumerable<T> methods
public partial interface IHandlers;

public class MyHandlers
{
    [Handler]
    public void DoWork() { }  // Generated method: Execute()

    [Handler]
    public string GetData() { }  // Generated method: Query()

    [Handler]
    public Task ProcessAsync() { }  // Generated method: ExecuteAsync()

    [Handler]
    public Task<int> GetCountAsync() { }  // Generated method: QueryAsync()

    [Handler]
    public IAsyncEnumerable<Item> GetItemsAsync() { }  // Generated method: Stream()
}
```

### CancellationToken Handling

For static methods with a single `CancellationToken` parameter, Terminus automatically generates a cancellation check:

```csharp
public static class MyHandlers
{
    [Handler]
    public static void Process(string data, CancellationToken ct) { }
}

// Generated:
void IFacade.Process(string data, CancellationToken ct)
{
    ct.ThrowIfCancellationRequested();
    global::MyNamespace.MyHandlers.Process(data, ct);
}
```

### Async Enumerable Support

For `IAsyncEnumerable<T>` return types on scoped facades, Terminus generates a proxy iterator:

```csharp
[FacadeOf(typeof(HandlerAttribute), Scoped = true)]
public partial interface IHandlers;

public class MyService
{
    [Handler]
    public async IAsyncEnumerable<Item> GetItemsAsync()
    {
        // Implementation
    }
}

// Generated:
async IAsyncEnumerable<Item> IHandlers.GetItemsAsync()
{
    await foreach (var item in _asyncScope.Value.ServiceProvider
        .GetRequiredService<MyService>()
        .GetItemsAsync())
    {
        yield return item;
    }
}
```

### Method Aggregation

Terminus supports automatic method aggregation, where multiple methods with identical signatures are combined into a single facade method. This enables notification/broadcast patterns similar to MediatR's `INotification` handlers.

#### Default Aggregation Behavior

**By default**, when multiple methods share the same signature (name, parameters, and generic constraints), they are automatically aggregated into a single facade method that executes all handlers in sequence.

**Void Methods (Commands/Notifications):**
```csharp
[FacadeOf(typeof(HandlerAttribute))]
public partial interface INotificationBus;

public class HandlerAttribute : Attribute { }

// Multiple handlers for the same notification type
public class EmailNotificationHandler
{
    [Handler]
    public void Handle(UserCreatedNotification notification)
    {
        // Send email
    }
}

public class LoggingNotificationHandler
{
    [Handler]
    public void Handle(UserCreatedNotification notification)
    {
        // Log event
    }
}

public class AuditNotificationHandler
{
    [Handler]
    public void Handle(UserCreatedNotification notification)
    {
        // Audit trail
    }
}

// Generated: Single method that executes all three handlers
void Handle(UserCreatedNotification notification);

// Usage - all handlers execute:
notificationBus.Handle(new UserCreatedNotification(userId, email));
```

**Result Methods (Queries):**

When handlers return results, the aggregated method returns `IEnumerable<T>` to yield all results:

```csharp
[FacadeOf(typeof(HandlerAttribute))]
public partial interface ISearchBus;

public class PrimarySearchHandler
{
    [Handler]
    public SearchResult Search(string query)
    {
        return new SearchResult("Primary", score: 10);
    }
}

public class SecondarySearchHandler
{
    [Handler]
    public SearchResult Search(string query)
    {
        return new SearchResult("Secondary", score: 5);
    }
}

// Generated: Returns IEnumerable<SearchResult>
IEnumerable<SearchResult> Search(string query);

// Usage - collect all results:
var results = searchBus.Search("query").OrderByDescending(r => r.Score).ToList();
```

**Async Result Methods:**

For async methods returning `Task<T>` or `ValueTask<T>`, the aggregated method returns `IAsyncEnumerable<T>`:

```csharp
[FacadeOf(typeof(HandlerAttribute))]
public partial interface IAsyncQueryBus;

public class DatabaseSearchHandler
{
    [Handler]
    public async Task<User> SearchAsync(string query)
    {
        return await database.FindUserAsync(query);
    }
}

public class CacheSearchHandler
{
    [Handler]
    public async Task<User> SearchAsync(string query)
    {
        return await cache.GetUserAsync(query);
    }
}

// Generated: Returns IAsyncEnumerable<User>
IAsyncEnumerable<User> SearchAsync(string query);

// Usage - stream all results:
await foreach (var user in asyncQueryBus.SearchAsync("john"))
{
    Console.WriteLine(user.Name);
}
```

#### Return Type Transformations

When methods are aggregated, return types are transformed to accommodate multiple results:

| Original Return Type | Aggregated Return Type | Behavior |
|---------------------|----------------------|----------|
| `void` | `void` | Executes all handlers in sequence, no return value |
| `T` | `IEnumerable<T>` | Yields result from each handler |
| `Task` | `Task` | Awaits all handlers in sequence |
| `ValueTask` | `Task` | Awaits all handlers in sequence |
| `Task<T>` | `IAsyncEnumerable<T>` | Yields awaited result from each handler |
| `ValueTask<T>` | `IAsyncEnumerable<T>` | Yields awaited result from each handler |
| `IAsyncEnumerable<T>` | N/A | Not aggregated (streaming not composable) |

#### Selective Aggregation with AggregationMode

The `AggregationMode` property provides fine-grained control over which return types should be aggregated. This is useful when you want to prevent accidental aggregation while still enabling it for specific patterns.

**Available Flags:**

```csharp
public enum FacadeAggregationMode
{
    None = 0,              // Default: aggregate all matching signatures
    Commands = 1 << 0,     // Aggregate void methods only
    Queries = 1 << 1,      // Aggregate result (T) methods only
    AsyncCommands = 1 << 2,    // Aggregate Task/ValueTask methods only
    AsyncQueries = 1 << 3,     // Aggregate Task<T>/ValueTask<T> methods only
    AsyncStreams = 1 << 4,     // Aggregate IAsyncEnumerable<T> methods only
    All = Commands | Queries | AsyncCommands | AsyncQueries | AsyncStreams
}
```

**Example: Aggregate Only Commands**

```csharp
[FacadeOf(typeof(HandlerAttribute),
    AggregationMode = FacadeAggregationMode.Commands)]
public partial interface ICommandBus;

public class UserHandlers
{
    [Handler]
    public void CreateUser(CreateUserCommand cmd) { }  // Will be aggregated

    [Handler]
    public void DeleteUser(DeleteUserCommand cmd) { }  // Will be aggregated

    [Handler]
    public string GetUser(GetUserQuery query) { }  // Separate method (not void)
}
```

**Example: Combine Multiple Flags**

```csharp
[FacadeOf(typeof(HandlerAttribute),
    AggregationMode = FacadeAggregationMode.Commands | FacadeAggregationMode.AsyncQueries)]
public partial interface IHybridBus;

// Only void methods and Task<T> methods will be aggregated
// Other return types generate separate facade methods
```

**Example: Disable Aggregation for Specific Return Types**

To prevent aggregation for specific return types while allowing default behavior for others, you can explicitly set which types should aggregate:

```csharp
// Aggregate everything EXCEPT queries (result methods)
[FacadeOf(typeof(HandlerAttribute),
    AggregationMode = FacadeAggregationMode.Commands |
                     FacadeAggregationMode.AsyncCommands |
                     FacadeAggregationMode.AsyncQueries)]
public partial interface IMediator;
```

#### When to Use Aggregation

**Good Use Cases:**
- **Notification patterns**: Multiple handlers reacting to the same event (audit, logging, email)
- **Multi-source queries**: Collecting results from multiple data sources (cache + database, primary + secondary search)
- **Side effects**: Triggering multiple independent actions (send email, update cache, log event)
- **Observability**: Multiple observers monitoring the same action

**Avoid Aggregation When:**
- Handlers have different purposes despite same signature (use different attributes instead)
- Order of execution matters critically (aggregation order is alphabetical by type name)
- Only one handler should execute (use separate methods or different signatures)
- Performance is critical and you need fine-grained control over execution

## Architecture Deep Dive

### Generator Pipeline

The generator follows a four-stage pipeline:

**1. Discovery Phase** (`Discovery/`)
   - `FacadeInterfaceDiscovery.IsCandidateFacadeInterface()`: Fast syntax check for partial interfaces with attributes
   - `FacadeInterfaceDiscovery.DiscoverFacadeInterface()`: Semantic analysis to find `[FacadeOf]` attributes
   - `FacadeMethodDiscovery.IsCandidateMethod()`: Fast syntax check for methods
   - `FacadeMethodDiscovery.DiscoverMethods()`: Semantic analysis to create `CandidateMethodInfo` for each attribute on each method

**2. Matching Phase** (`Matching/`)
   - `FacadeMethodMatcher.MatchMethodsToFacade()`: Filters methods where attribute inherits from facade's specified attribute types
   - Supports attribute inheritance (derived attributes match)

**2.5. Grouping Phase** (`Grouping/`)
   - `MethodSignatureGrouper.GroupBySignature()`: Groups methods by signature for aggregation
   - `AggregatedMethodGroup`: Represents grouped methods with shared signatures
   - Respects `AggregationMode` flags to control which return types get aggregated

**3. Validation Phase**
   - `UsageValidator.Validate()`: Static utility that orchestrates validation using a `CompositeMethodValidator` which wraps specialized validators implementing `IMethodValidator`.
   - Validation is performed in a single pass over discovered methods.
   - Validators include:
     - `RefOrOutParameterValidator`: Checks for unsupported `ref`, `out`, or `in` parameters (**TM0002**).
     - `DuplicateSignatureValidator`: Detects duplicate method signatures within the same facade, accounting for name, parameters, and generic constraints (**TM0001**).
     - `ConflictingNameValidator`: Prevents name collisions with internal implementation fields like `_serviceProvider` (**TM0003**).
   - Reports diagnostics via `SourceProductionContext`
   - Skips code generation if errors found

**4. Generation Phase** (`Builders/`)
   - `FacadeGenerationContext`: Immutable context containing facade info and matched methods
   - `FacadeBuilderOrchestrator`: Top-level builder coordinating generation
   - `NamespaceBuilder`: Builds namespace with interface and implementation
   - `InterfaceBuilder`: Builds partial interface with method signatures
   - `ImplementationClassBuilder`: Orchestrates building of implementation class
   - `MethodBuilder`: Builds individual method implementations

### Builder System

The builder system is modular and composable:

```
FacadeBuilderOrchestrator
└─ NamespaceBuilder
   ├─ InterfaceBuilder
   │  └─ MethodSignatureBuilder (interface methods)
   └─ ImplementationClassBuilder
      ├─ FieldBuilder (fields)
      ├─ ConstructorBuilder (constructor)
      ├─ MethodBuilder (methods)
      │  ├─ MethodSignatureBuilder (implementation method stubs)
      │  └─ MethodBodyBuilder
      │     └─ InvocationBuilder
      │        └─ ServiceResolutionStrategyFactory
      │           ├─ StaticServiceResolution
      │           ├─ ScopedServiceResolution
      │           └─ NonScopedServiceResolution
      └─ DisposalBuilder (Dispose/DisposeAsync for scoped)
```

**Key Builders:**

- **NamespaceBuilder**: Top-level, creates namespace with interface and class
- **InterfaceBuilder**: Generates partial interface definition
- **ImplementationClassBuilder**: Generates sealed implementation class with:
  - `[FacadeImplementation]` attribute (for non-scoped or scoped with instance methods)
  - Base interfaces (`IDisposable`, `IAsyncDisposable` for scoped)
  - Fields (via `FieldBuilder`)
  - Constructor (via `ConstructorBuilder`)
  - Methods (via `MethodBuilder`)
  - Disposal methods (via `DisposalBuilder` for scoped)
- **MethodSignatureBuilder**: Builds method signatures (return type, name, parameters)
- **MethodBodyBuilder**: Builds method body statements
- **InvocationBuilder**: Builds method invocation expressions with `ConfigureAwait(false)`
- **FieldBuilder**: Generates field declarations (different for scoped vs non-scoped)
- **ConstructorBuilder**: Generates constructors (different for scoped vs non-scoped)
- **DisposalBuilder**: Generates `Dispose()` and `DisposeAsync()` for scoped facades

### Code Generation Approach

Terminus uses a **hybrid approach** combining raw string interpolation with Roslyn `SyntaxFactory`:

**Raw String Templates** (for readability):
```csharp
var constructor = ParseMemberDeclaration(
    $$"""
      public {{implementationClassName}}(IServiceProvider serviceProvider)
      {
          _serviceProvider = serviceProvider;
      }
      """)!;
```

**SyntaxFactory** (for dynamic composition):
```csharp
var classDeclaration = ClassDeclaration(implementationClassName)
    .WithModifiers([Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword)])
    .AddBaseListTypes(SimpleBaseType(ParseTypeName(interfaceName)));
```

**Composition Root**:
```csharp
public static CompilationUnitSyntax Generate(FacadeGenerationContext context)
{
    var namespaceDeclaration = NamespaceBuilder.Build(/*...*/);
    var namespaceCode = namespaceDeclaration.ToFullString().TrimStart();

    var rawCompilationUnit =
      $$"""
        // <auto-generated/> Generated by Terminus FacadeGenerator
        #nullable enable
        {{namespaceCode}}
        """;

    return ParseCompilationUnit(rawCompilationUnit);
}
```

**Guidelines:**
- Use raw strings for static/templated code chunks (easier to read)
- Use `SyntaxFactory` for dynamic composition (type-safe)
- Use `SyntaxFactory` parsers (`ParseMemberDeclaration`, `ParseTypeName`, etc.) for static chunks
- Always call `.NormalizeWhitespace()` on final syntax nodes
- Keep composition root as a template for readability

## Development Guidelines

### Generator Development

**Key Generator Pattern:**
```csharp
[Generator]
public class FacadeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Two-phase discovery: fast syntax filter, then semantic analysis
        var discoveredFacades = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => FacadeInterfaceDiscovery.IsCandidateFacadeInterface(node),
                transform: static (ctx, ct) => FacadeInterfaceDiscovery.DiscoverFacadeInterface(ctx, ct))
            .Where(static m => m.HasValue)
            .Select((m, _) => m!.Value)
            .Collect();

        var discoveredMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => FacadeMethodDiscovery.IsCandidateMethod(node),
                transform: static (ctx, ct) => FacadeMethodDiscovery.DiscoverMethods(ctx, ct))
            .Where(static m => m.HasValue && !m.Value.IsEmpty)
            .SelectMany((m, _) => m!.Value)
            .Collect();

        var combined = discoveredFacades.Combine(discoveredMethods);
        context.RegisterSourceOutput(combined, FacadeGenerationPipeline.Execute);
    }
}
```

**Two-phase discovery:**
1. **Syntax predicate**: Fast filter (e.g., partial interfaces with attributes, methods)
2. **Semantic transform**: Semantic analysis to check if attribute matches target type

### Diagnostics

Define diagnostics in `Diagnostics.cs`:

```csharp
public static readonly DiagnosticDescriptor DuplicateFacadeMethodSignature = new(
    id: "TM0001",
    title: "Duplicate entry point signature",
    messageFormat: "Duplicate entry point signature detected for method '{0}'",
    category: "Terminus.Generator",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Entry point methods must have unique signatures within the same attribute type.");
```

Report diagnostics in validation:

```csharp
var diagnostic = Diagnostic.Create(
    Diagnostics.DuplicateFacadeMethodSignature,
    method.Locations.FirstOrDefault(),
    method.Name);
context.ReportDiagnostic(diagnostic);
```

### Testing Strategy

**IMPORTANT: This project follows Test-Driven Development (TDD)**

When adding new features to the generator:

1. **Write Failing Tests First**
   - Update ALL affected test expectations in the relevant test files with the new expected output
   - Include the new feature in the expected generated code
   - Do NOT implement the feature yet

2. **Run Tests to Confirm Failure**
   - Run `dotnet test` to verify tests fail with clear diff showing what's missing
   - The diff output shows exactly what needs to be implemented

3. **Implement the Minimum Code**
   - Implement ONLY what's needed to make the tests pass
   - Follow the existing builder pattern and code organization

4. **Run Tests to Verify Success**
   - Run `dotnet test` to verify all tests pass
   - Build the solution to ensure no warnings or errors

5. **Refactor if Needed**
   - Clean up code while keeping tests green
   - Follow existing patterns and conventions

**Test Infrastructure:**

All generator tests inherit from `TerminusSourceGeneratorTest<T>`:

```csharp
var test = new TerminusSourceGeneratorTest<FacadeGenerator>
{
    TestState =
    {
        Sources = { inputSource }
    }
};

test.TestState.GeneratedSources.Add(
    (typeof(FacadeGenerator), "Demo_IFacade_Generated.g.cs",
     SourceText.From(expectedOutput, Encoding.UTF8)));

await test.RunAsync();
```

**What to test:**
- Single method with parameters
- Multiple methods with different signatures
- Static vs instance methods
- Methods with no parameters
- Methods with various return types (void, T, Task, Task<T>, IAsyncEnumerable<T>)
- Scoped vs non-scoped facades
- Custom naming (CommandName, QueryName, etc.)
- Method aggregation with multiple handlers sharing signatures
- Selective aggregation with AggregationMode flags
- Return type transformations for aggregated methods
- Error cases (duplicate signatures, ref/out parameters)
- Generic methods with constraints
- Type parameter propagation in facade methods

**Test structure:**
```csharp
[Fact]
public async Task Given_X_Should_generate_Y()
{
    const string source = """...""";
    const string expected = """...""";

    var test = new TerminusSourceGeneratorTest<FacadeGenerator>
    {
        TestState = { Sources = { source } }
    };

    test.TestState.GeneratedSources.Add((generator, filename, expected));
    await test.RunAsync();
}
```

**TDD Example: Adding [GeneratedCode] Attribute**

This feature was implemented using TDD:

1. **Updated all test expectations** to include `[GeneratedCode("Terminus.Generator", "1.0.0")]` on both interface and implementation class
2. **Ran tests** - they failed with clear diffs showing missing attributes (lines prefixed with `-`)
3. **Created helper classes**:
   - `GeneratorVersion.cs` - Extracts version from assembly
   - `GeneratedCodeAttributeBuilder.cs` - Builds the attribute syntax
4. **Updated builders**:
   - `InterfaceBuilder.cs` - Added attribute to interface
   - `ImplementationClassBuilder.cs` - Added attribute to implementation class
5. **Ran tests** - all passed (14 tests on .NET 8.0/10.0, 12 on .NET Framework 4.7.2)

**Benefits of TDD Approach:**
- Ensures test coverage for new features from the start
- Provides clear specification of expected behavior in test code
- Catches regressions immediately
- Makes refactoring safer (tests act as safety net)
- Forces thinking about design before implementation

### Common Patterns

**1. Attribute inheritance checking:**
```csharp
private static bool InheritsFromAttribute(
    INamedTypeSymbol attributeClass,
    INamedTypeSymbol targetAttributeType)
{
    var current = attributeClass;
    while (current != null)
    {
        if (SymbolEqualityComparer.Default.Equals(current, targetAttributeType))
            return true;
        current = current.BaseType;
    }
    return false;
}
```

**2. Symbol equality:**
Always use `SymbolEqualityComparer.Default` when comparing symbols:
```csharp
if (SymbolEqualityComparer.Default.Equals(typeSymbol1, typeSymbol2))
```

**3. Type name serialization:**
Use `ToDisplayString()` with `SymbolDisplayFormat.FullyQualifiedFormat`:
```csharp
var typeName = methodSymbol.ContainingType
    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
// Result: "global::MyNamespace.MyClass"
```

**4. Return type detection:**
```csharp
public static ReturnTypeKind ResolveReturnTypeKind(this Compilation compilation, IMethodSymbol method)
{
    if (method.ReturnsVoid) return ReturnTypeKind.Void;

    var returnType = method.ReturnType;
    var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");

    if (SymbolEqualityComparer.Default.Equals(returnType, taskType))
        return ReturnTypeKind.Task;

    // ... more checks
}
```

### Naming Conventions

- **Generated implementation classes**: `{InterfaceName}_Generated` (e.g., `IFacade_Generated`)
- **Generated files**: `{Namespace}_{InterfaceName}_Generated.g.cs` (e.g., `Demo_IFacade_Generated.g.cs`)
- **Namespace**: Same namespace as the facade interface
- **File naming helper**: `symbol.ToIdentifierString()` (replaces `.` with `_`)

### Code Style

- **Favor C# 14+ features** where possible, so long as they are compatible with the baseline framework supported, or are otherwise polyfilled
- Use modern C# features appropriately (file-scoped namespaces, raw strings, collection expressions, primary constructors, etc.)
- Use `readonly` for fields that don't change
- Prefer expression-bodied members for simple methods
- Use `var` for obvious types
- Use explicit types for clarity in complex scenarios
- Use `static` on builder classes when possible
- Use primary constructors for simple classes

### Error Handling

- Generator should not throw exceptions (use diagnostics instead)
- Validate input early in the pipeline
- Skip code generation if validation errors exist
- Include helpful information in diagnostic messages (method name, parameter name, etc.)

## Usage Examples

### Basic Facade

```csharp
// 1. Define attribute
public class HandlerAttribute : Attribute { }

// 2. Define facade interface
[FacadeOf(typeof(HandlerAttribute))]
public partial interface IHandlers;

// 3. Define implementation
public class MyHandlers
{
    [Handler]
    public void Process(string data)
    {
        Console.WriteLine($"Processing: {data}");
    }
}

// 4. Register facades with DI container
services.AddTerminusFacades();

// 5. Use facade
var facade = serviceProvider.GetRequiredService<IHandlers>();
facade.Process("hello");
```

### Dependency Injection Registration

Terminus provides automatic registration of generated facades with `IServiceCollection` via the `AddTerminusFacades()` extension method:

```csharp
using Microsoft.Extensions.DependencyInjection;

// Register all facades from calling assembly
// - Disposable facades (IDisposable/IAsyncDisposable) → Scoped
// - Non-disposable facades → Transient
services.AddTerminusFacades();

// Register facades from specific assemblies
services.AddTerminusFacades(typeof(IMyFacade).Assembly, typeof(IOtherFacade).Assembly);

// Register with explicit lifetime (overrides defaults)
services.AddTerminusFacades(ServiceLifetime.Singleton);
services.AddTerminusFacades(ServiceLifetime.Scoped, typeof(IMyFacade).Assembly);
```

**How it works:**
- Extension method is in `Microsoft.Extensions.DependencyInjection` namespace (automatically available)
- Scans assemblies for types decorated with `[FacadeImplementation]`
- Registers each implementation with its corresponding interface
- Automatically determines lifetime based on disposal patterns:
  - **Scoped**: Facades implementing `IDisposable` or `IAsyncDisposable`
  - **Transient**: All other facades (safe default for stateless facades)
- Allows explicit lifetime override for advanced scenarios

**Example:**
```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register handler services
builder.Services.AddScoped<MyHandlers>();

// Auto-register all facades - extension method is automatically available
builder.Services.AddTerminusFacades();

var app = builder.Build();

// Use facades in endpoints via dependency injection
app.MapGet("/process", (IHandlers handlers, string data) =>
{
    handlers.Process(data);
    return Results.Ok();
});
```

### Scoped Facade with Async Methods

```csharp
[FacadeOf(typeof(HandlerAttribute), Scoped = true)]
public partial interface IHandlers;

public class MyHandlers
{
    private readonly IDatabase _db;

    public MyHandlers(IDatabase db) => _db = db;

    [Handler]
    public async Task<User> GetUserAsync(int id)
    {
        return await _db.Users.FindAsync(id);
    }

    [Handler]
    public async IAsyncEnumerable<User> GetAllUsersAsync()
    {
        await foreach (var user in _db.Users.AsAsyncEnumerable())
            yield return user;
    }
}

// Usage
await using var facade = new IHandlers_Generated(serviceProvider);
var user = await facade.GetUserAsync(123);
await foreach (var u in facade.GetAllUsersAsync())
{
    Console.WriteLine(u.Name);
}
// Scope is automatically disposed here
```

### Custom Method Naming

```csharp
[FacadeOf(typeof(HandlerAttribute),
    CommandName = "Execute",
    QueryName = "Query",
    AsyncCommandName = "ExecuteAsync",
    AsyncQueryName = "QueryAsync")]
public partial interface IHandlers;

public class MyHandlers
{
    [Handler]
    public void DoWork() { }  // Facade method: Execute()

    [Handler]
    public string GetData() { }  // Facade method: Query()

    [Handler]
    public Task ProcessAsync() { }  // Facade method: ExecuteAsync()

    [Handler]
    public Task<int> ComputeAsync() { }  // Facade method: QueryAsync()
}
```

### Multiple Attribute Types

```csharp
public class CommandAttribute : Attribute { }
public class QueryAttribute : Attribute { }

[FacadeOf(typeof(CommandAttribute), typeof(QueryAttribute))]
public partial interface IHandlers;

public class MyHandlers
{
    [Command]
    public void Execute(string cmd) { }

    [Query]
    public string Get(int id) { }
}

// Both methods appear in IHandlers facade
```

### Static and Instance Methods

```csharp
[FacadeOf(typeof(HandlerAttribute))]
public partial interface IHandlers;

public class InstanceHandlers
{
    [Handler]
    public void InstanceMethod() { }  // Resolved from DI
}

public static class StaticHandlers
{
    [Handler]
    public static void StaticMethod() { }  // Called directly
}
```

## Quick Reference

### Key Files to Understand

**For Generator Development:**
1. `FacadeGenerator.cs` - Entry point for source generation
2. `Pipeline/FacadeGenerationPipeline.cs` - Orchestrates discovery → matching → grouping → validation → generation
3. `Builders/FacadeBuilderOrchestrator.cs` - Top-level builder
4. `Discovery/FacadeInterfaceDiscovery.cs` - Discovers `[FacadeOf]` interfaces
5. `Discovery/FacadeMethodDiscovery.cs` - Discovers methods with attributes
6. `Matching/FacadeMethodMatcher.cs` - Matches methods to facades
7. `Grouping/MethodSignatureGrouper.cs` - Groups methods by signature for aggregation
8. `AggregatedMethodGroup.cs` - Represents method groups for aggregation
9. `UsageValidator.cs` - Orchestrates validation
10. `Validation/` - Specialized method validators implementing `IMethodValidator`
11. `Builders/Strategies/ServiceResolutionStrategyFactory.cs` - Selects resolution strategy

**For Understanding Generated Code:**
1. `Builders/Interface/InterfaceBuilder.cs` - Generates partial interface
2. `Builders/Class/ImplementationClassBuilder.cs` - Generates implementation class
3. `Builders/Method/MethodBuilder.cs` - Generates method implementations
4. `Builders/Method/MethodBodyBuilder.cs` - Generates method bodies
5. `Builders/Method/InvocationBuilder.cs` - Generates method invocations

### Common Tasks

**Add a new service resolution strategy:**
→ Create class implementing `IServiceResolutionStrategy` in `Builders/Strategies/`
→ Add to `ServiceResolutionStrategyFactory.Strategies` array

**Modify generated code:**
→ Edit appropriate builder in `Builders/` folder
→ Update expected strings in `FacadeGeneratorTests.cs`

**Add new diagnostic:**
→ Define in `Diagnostics.cs`
→ Report in `UsageValidator.cs` or relevant location

**Test generator changes:**
→ Add test case to `FacadeGeneratorTests.cs`
→ Provide input source and expected generated output

**Modify aggregation behavior:**
→ Edit `Grouping/MethodSignatureGrouper.cs`
→ Update `ShouldAggregate()` logic for new aggregation rules
→ Update tests in `AggregationTests.cs` and `AggregationModeTests.cs`

**Debug generation:**
→ Set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` in consumer `.csproj`
→ Check `obj/Debug/netX.0/generated/` folder

**Support new return type:**
→ Add case to `ReturnTypeKind` enum
→ Update `CompilationExtensions.ResolveReturnTypeKind()`
→ Update `MethodBodyBuilder.BuildMethodBody()` to handle new case
→ Update `MethodSignatureBuilder.BuildImplementationMethodStub()` if async modifier needed

## Future Considerations

### Potential Enhancements

1. **Ref/out/in parameter support**: Currently not supported (TM0002 error)
2. **Source link support**: Map generated code back to original methods
4. **Configuration API**: Builder pattern for complex facade configuration
5. **Multiple facade implementations**: Generate different implementations for same interface
6. **Method overload disambiguation**: Handle methods with same name but different parameter types
7. **Attribute-based parameter binding**: Custom parameter resolution strategies
8. **Escaping for reserved keywords**: Added basic escaping for parameter and type parameter names.

### Performance Optimizations

- Cache method info lookups
- Optimize scope creation for mixed sync/async scenarios
- Consider pooling for frequently created scopes
- Benchmark generated code vs manual implementations

---

## Important Instruction Reminders

- **DO** use `SyntaxFactory` parsers (`ParseMemberDeclaration`, `ParseTypeName`, etc.) to create strongly typed chunks of static or easily templated source snippets
- **DO** try to define the composition root as a templated string (for readability)
- **DO** use `SyntaxFactory` for composition of highly dynamic code snippets
- **DO NOT** use `StringBuilder` to compose code snippets when generating source using Roslyn
- When writing multiline strings in C#, use the raw string literal syntax (`"""`)
- Keep the opening and closing `"""` vertically aligned by starting on a new line

### Additional Guidelines

- Do what has been asked; nothing more, nothing less
- **NEVER** create files unless they're absolutely necessary for achieving your goal
- **ALWAYS** prefer editing an existing file to creating a new one
- **NEVER** proactively create documentation files (`*.md`) or README files unless explicitly requested

