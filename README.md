# Terminus

Terminus is a source generator that aggregates methods from multiple types into a single facade.

## Examples

### Basic Example
A simple project demonstrating how to aggregate methods from multiple services into one facade.
See [examples/Terminus.Example.Basic](examples/Terminus.Example.Basic).

#### How to run:
```bash
dotnet run --project examples/Terminus.Example.Basic/Terminus.Example.Basic.csproj
```

#### Key Concepts:
1. **Custom Attribute**: Define an attribute (e.g., `[FacadeMethod]`) to mark methods you want to include.
2. **Services**: Decorate methods in your services with your custom attribute.
3. **Facade Interface**: Create a `partial interface` and decorate it with `[FacadeOf(typeof(YourAttribute))]`.
4. **Registration**: Use `services.AddTerminusFacades()` to automatically register the generated facade.