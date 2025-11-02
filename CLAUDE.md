# Terminus - AI Context Documentation

## Project Overview

**Terminus** is a C# source generator framework that implements a compile-time mediator pattern for method discovery and invocation. It provides a type-safe, reflection-free way to discover and invoke methods marked with custom attributes at runtime.

### Key Features

- **Compile-time code generation**: Uses Roslyn source generators to discover and generate mediator code
- **Type-safe mediation**: Generates strongly-typed mediator interfaces for method invocation
- **Flexible parameter binding**: Extensible parameter resolution system with custom binding strategies
- **Attribute-based discovery**: Methods marked with `[EntryPoint]` (or derived attributes) are automatically discovered
- **Dependency injection integration**: Seamless integration with Microsoft.Extensions.DependencyInjection
- **Multi-framework support**: Targets .NET 7.0+, with specific support for net472 in tests

### Core Value Proposition

Traditional mediator patterns rely on runtime reflection or manual registration. Terminus moves this work to compile-time, generating:
1. Mediator interfaces with strongly-typed method signatures
2. Service registration code that wires up entry points
3. Invocation logic with parameter resolution and scope management

This eliminates runtime overhead, provides compile-time safety, and enables rich tooling support.

## Project Structure

```
terminus/
├── src/
│   ├── Terminus.Attributes/           # Attribute definitions
│   │   ├── EntryPointAttribute.cs     # Base attribute for marking entry point methods
│   │   ├── EntryPointMediatorAttribute.cs  # Marks interfaces to generate mediator implementations
│   │   └── ParameterBinderAttribute.cs     # Base for custom parameter binding attributes
│   │
│   ├── Terminus.Generator/            # Source generators
│   │   ├── EndpointDiscoveryGenerator.cs        # Discovers [EntryPoint] methods
│   │   ├── EntrypointRegistrationSourceBuilder.cs  # Builds generated source code
│   │   └── EntryPointMethodInfo.cs              # Model for discovered methods
│   │
│   └── Terminus/                      # Runtime library
│       ├── EntryPointDescriptor.cs    # Runtime descriptor for entry point methods
│       ├── ParameterBindingStrategyResolver.cs  # Resolves parameters using strategies
│       ├── ParameterBindingContext.cs # Context passed to binding strategies
│       ├── IParameterBinder.cs        # Interface for custom parameter binders
│       └── Strategies/                # Built-in binding strategies
│           ├── IParameterBindingStrategy.cs
│           ├── DependencyInjectionBindingStrategy.cs
│           ├── CancellationTokenBindingStrategy.cs
│           └── ParameterNameBindingStrategy.cs
│
├── examples/
│   └── Terminus.Generator.Examples.HelloWord/  # Example implementation
│
└── test/
    └── Terminus.Generator.Tests.Unit/  # Generator unit tests
        └── Generator/
            ├── EndpointDiscoveryGeneratorTests.cs
            └── Infrastructure/
                └── TerminusSourceGeneratorTest.cs  # Test base class

```

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
public partial class Mediator_IMediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

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

You can create custom attributes deriving from `EntryPointAttribute`:

```csharp
public class CommandAttribute : EntryPointAttribute { }

[EntryPointMediator(typeof(CommandAttribute))]
public partial interface ICommandMediator;

public class MyCommands
{
    [Command]
    public void Execute() { }
}
```

The generator discovers methods with derived attributes and groups them by attribute type.

### Type Resolution

- **Static methods**: Invoked directly on the type
- **Instance methods**: Type is resolved from DI container per invocation
- **Scoped invocation**: Each mediator method call creates a new DI scope

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

- **Generated classes**: `Mediator_{InterfaceName}` (e.g., `Mediator_IMediator`)
- **Generated file**: `EntryPoints.g.cs`
- **Namespace**: All generated code lives in `Terminus.Generated`
- **Extension methods**: `ServiceCollectionExtensions.AddEntryPoints()`

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
   - Calls `EntrypointRegistrationSourceBuilder.Generate()`
   - Produces single `EntryPoints.g.cs` file

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
3. Optionally enhance `EntryPointMediatorAttribute` to specify which attribute type to mediate
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

1. **Return value handling**: Currently only void methods, could support async and return values
2. **Method overload support**: Currently assumes unique method names
3. **Attribute-based parameter binding**: Mark parameters with attributes for custom binding
4. **Diagnostics**: Report errors/warnings during generation (missing services, ambiguous methods)
5. **Source link support**: Map generated code back to original methods
6. **Batch invocation**: Invoke multiple entry points in a transaction
7. **Interceptors**: Pre/post processing hooks for entry point invocation

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

1. `EndpointDiscoveryGenerator.cs` - Entry point for source generation
2. `EntrypointRegistrationSourceBuilder.cs` - Code generation logic
3. `ParameterBindingStrategyResolver.cs` - Parameter resolution
4. `EntryPointDescriptor.cs` - Runtime method descriptor
5. `TerminusSourceGeneratorTest.cs` - Test infrastructure

### Common Tasks

**Add a new strategy:**
→ Create class implementing `IParameterBindingStrategy` in `Strategies/`

**Modify generated code:**
→ Edit `EntrypointRegistrationSourceBuilder.cs`

**Test generator changes:**
→ Update expected strings in `EndpointDiscoveryGeneratorTests.cs`

**Debug generation:**
→ Set `EmitCompilerGeneratedFiles=true` and check `obj/generated/`

**Add new attribute:**
→ Create in `Terminus.Attributes/`, inherit from `EntryPointAttribute`
- use context7
- DO NOT use StringBuilder to compose code snippets when generating source using Roslyn
- When using Roslyn, DO use SyntaxFactory for composition of highly dynamic code snippets
- When using Roslyn, DO use the SyntaxFactory parsers to create strongly type chunks of static or easily templated source snippets
- DO (when using Roslyn) try and define the composition root as as a templated string (for readability).  If templating beocmes excessive then use a SyntaxFactory
- When writing multiline strings in C#, use the raw string literal syntax (""").  Please keep the open and closing """ (and variants) vertically aligned by starting on a new line (particularly when doing assignments)