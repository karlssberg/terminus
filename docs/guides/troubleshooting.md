# Troubleshooting

This guide helps resolve common issues when using Terminus.

## Compilation Errors

### TM0001: Duplicate Entry Point Signature

**Error:**
```
Error TM0001: Duplicate entry point signature detected for method 'Process'
```

**Cause:** Two methods with the same signature are marked with the same attribute.

**Solution:** Make method signatures unique by changing:
- Method name
- Parameter types or count
- Generic type parameters

```csharp
// ❌ Duplicate signatures
public class ServiceA
{
    [Handler]
    public void Process(string data) { }
}

public class ServiceB
{
    [Handler]
    public void Process(string data) { }  // Error TM0001
}

// ✅ Fix: Make signatures unique
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

// ✅ Alternative: Different parameters
public class ServiceA
{
    [Handler]
    public void Process(string data) { }
}

public class ServiceB
{
    [Handler]
    public void Process(int id, string data) { }  // OK - different signature
}
```

### TM0002: Ref/Out Parameter Not Supported

**Error:**
```
Error TM0002: Method 'TryGetUser' has 'ref', 'out', or 'in' parameters which are not supported
```

**Cause:** Methods with `ref`, `out`, or `in` parameters cannot be included in facades.

**Solution:** Use alternative patterns:

```csharp
// ❌ Not supported
[Handler]
public bool TryGetUser(int id, out User user) { ... }

// ✅ Fix: Return nullable
[Handler]
public User? TryGetUser(int id) { ... }

// ✅ Fix: Use Result wrapper
[Handler]
public Result<User> GetUser(int id)
{
    var user = _repo.Find(id);
    return user != null
        ? Result<User>.Success(user)
        : Result<User>.NotFound();
}

// ✅ Fix: Use tuple
[Handler]
public (bool Success, User? User) TryGetUser(int id)
{
    var user = _repo.Find(id);
    return user != null ? (true, user) : (false, null);
}
```

### TM0003: Conflicting Parameter or Type Parameter Name

**Error:**
```
Error TM0003: Method 'Process' has a parameter or type parameter named '_serviceProvider' which conflicts with internal implementation
```

**Cause:** Parameter or type parameter name conflicts with internal facade fields.

**Reserved names:**
- `_serviceProvider`
- `_syncScope`
- `_asyncScope`
- `_syncDisposed`
- `_asyncDisposed`

**Solution:** Rename the parameter:

```csharp
// ❌ Conflicts with internal field
[Handler]
public void Process(string _serviceProvider) { }

// ✅ Fix: Use different name
[Handler]
public void Process(string provider) { }
```

## Runtime Errors

### Service Not Registered

**Error:**
```
InvalidOperationException: Unable to resolve service for type 'MyService'
```

**Cause:** Service containing the handler method is not registered in DI.

**Solution:**

```csharp
// ❌ Missing service registration
services.AddTerminusFacades();
var facade = provider.GetRequiredService<IAppFacade>();
facade.DoWork();  // Runtime error

// ✅ Fix: Register all services
services.AddTransient<MyService>();  // Register service
services.AddTerminusFacades();       // Register facades
```

### Facade Not Found

**Error:**
```
InvalidOperationException: Unable to resolve service for type 'IAppFacade'
```

**Cause:** Facade not registered with DI.

**Solution:**

```csharp
// ❌ Forgot to register facades
services.AddTransient<MyService>();
var facade = provider.GetRequiredService<IAppFacade>();  // Error

// ✅ Fix: Call AddTerminusFacades()
services.AddTransient<MyService>();
services.AddTerminusFacades();  // Register all facades
var facade = provider.GetRequiredService<IAppFacade>();  // Works
```

### ObjectDisposedException

**Error:**
```
ObjectDisposedException: Cannot access a disposed object.
```

**Cause:** Scoped facade was disposed before async operation completed.

**Solution:**

```csharp
// ❌ Facade disposed too early
IAppFacade facade;
await using (facade = provider.GetRequiredService<IAppFacade>())
{
    var task = facade.GetDataAsync();
} // Facade disposed here
await task; // Error: facade already disposed

// ✅ Fix: Await before disposal
await using (var facade = provider.GetRequiredService<IAppFacade>())
{
    var result = await facade.GetDataAsync();
    // Use result
} // Safe to dispose
```

## Build Issues

### Generated Code Not Found

**Symptom:** IntelliSense doesn't show facade methods, or build fails with "does not contain a definition".

**Cause:** Generated code not compiled or project not rebuilt.

**Solution:**

```bash
# Clean and rebuild
dotnet clean
dotnet build

# If still not working, try:
rm -rf bin obj
dotnet build
```

### Partial Interface Not Recognized

**Symptom:**
```
Error CS0260: Missing partial modifier on declaration of type 'IAppFacade'
```

**Cause:** Interface not marked as `partial`.

**Solution:**

```csharp
// ❌ Missing partial keyword
[FacadeOf<HandlerAttribute>]
public interface IAppFacade { }  // Error

// ✅ Fix: Add partial keyword
[FacadeOf<HandlerAttribute>]
public partial interface IAppFacade { }  // OK
```

### Generator Not Running

**Symptom:** No generated files, no errors, facade just empty.

**Causes & Solutions:**

1. **Wrong .NET SDK version:**
   ```bash
   dotnet --version  # Should be 6.0 or later
   ```

2. **Terminus not installed:**
   ```bash
   dotnet add package Terminus
   ```

3. **Generator disabled:**
   Check `.csproj` doesn't have:
   ```xml
   <!-- ❌ Don't disable generators -->
   <PropertyGroup>
       <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
   </PropertyGroup>
   ```

4. **IDE cache issue:**
   - Visual Studio: Restart IDE, clean solution
   - Rider: File → Invalidate Caches → Restart
   - VS Code: Reload window (Ctrl+Shift+P → "Reload Window")

## IntelliSense Issues

### Methods Not Appearing

**Symptom:** Facade methods don't show up in IntelliSense.

**Solutions:**

1. **Rebuild project:**
   ```bash
   dotnet build
   ```

2. **Restart IDE** (Visual Studio, Rider, VS Code)

3. **Check generated files exist:**
   ```bash
   ls obj/Debug/net8.0/generated/Terminus.Generator/
   ```

4. **Verify attribute is correct:**
   ```csharp
   [FacadeOf<HandlerAttribute>]  // Correct attribute type?
   public partial interface IAppFacade { }

   public class MyService
   {
       [Handler]  // Same attribute type?
       public void DoWork() { }
   }
   ```

### Stale IntelliSense

**Symptom:** IntelliSense shows old method signatures after changes.

**Solution:**
1. Clean and rebuild:
   ```bash
   dotnet clean
   dotnet build
   ```

2. Restart IDE

## Performance Issues

### Slow DI Resolution

**Symptom:** Facade method calls are slow.

**Cause:** Service resolution overhead for non-scoped facades.

**Solutions:**

1. **Use scoped facades** for multiple related calls:
   ```csharp
   [FacadeOf<HandlerAttribute>(Scoped = true)]
   public partial interface IAppFacade { }
   ```

2. **Use static methods** when possible:
   ```csharp
   public static class Helpers
   {
       [Handler]
       public static int Calculate(int x) => x * 2;  // No DI lookup
   }
   ```

3. **Register services as Singleton** if stateless:
   ```csharp
   services.AddSingleton<MyService>();
   ```

### Memory Leaks

**Symptom:** Memory usage grows over time.

**Causes & Solutions:**

1. **Scoped facades not disposed:**
   ```csharp
   // ❌ Leak
   var facade = provider.GetRequiredService<IAppFacade>();
   await facade.DoWorkAsync();
   // Never disposed!

   // ✅ Fix
   await using var facade = provider.GetRequiredService<IAppFacade>();
   await facade.DoWorkAsync();
   // Automatically disposed
   ```

2. **Long-lived ServiceProvider:**
   ```csharp
   // ❌ Leak
   var provider = services.BuildServiceProvider();
   // Never disposed!

   // ✅ Fix
   using var provider = services.BuildServiceProvider();
   // Disposed at end of scope
   ```

## Debugging Tips

### View Generated Code

Add to `.csproj`:
```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)\generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Then check: `obj/Debug/net8.0/generated/Terminus.Generator/`

### Enable Detailed Logging

```csharp
var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

### Use Debugger

1. Set breakpoint in your service method
2. Call facade method
3. Step through service implementation

### Check Facade Registration

```csharp
var facade = provider.GetService<IAppFacade>();
if (facade == null)
{
    Console.WriteLine("Facade not registered!");
}
else
{
    Console.WriteLine($"Facade type: {facade.GetType().Name}");
    // Should be: IAppFacade_Generated
}
```

## Common Mistakes

### 1. Forgetting `partial` Keyword

```csharp
// ❌ Error
[FacadeOf<HandlerAttribute>]
public interface IAppFacade { }

// ✅ Correct
[FacadeOf<HandlerAttribute>]
public partial interface IAppFacade { }
```

### 2. Wrong Attribute Type

```csharp
[FacadeOf<HandlerAttribute>]
public partial interface IAppFacade { }

public class MyService
{
    [Command]  // ❌ Wrong attribute - won't be discovered
    public void DoWork() { }
}
```

### 3. Not Registering Services

```csharp
// ❌ Missing service registration
services.AddTerminusFacades();

// ✅ Register services first
services.AddTransient<MyService>();
services.AddTerminusFacades();
```

### 4. Using ref/out Parameters

```csharp
// ❌ Not supported
[Handler]
public bool TryGet(int id, out User user) { ... }

// ✅ Use alternative
[Handler]
public User? TryGet(int id) { ... }
```

### 5. Blocking on Async

```csharp
// ❌ Deadlock risk
[Handler]
public User GetUser(int id)
{
    return GetUserAsync(id).Result;  // Don't block!
}

// ✅ Async all the way
[Handler]
public async Task<User> GetUserAsync(int id)
{
    return await _repo.GetAsync(id);
}
```

## Getting Help

If you're still stuck:

1. **Check examples:** Review [examples](../examples/basic.md) for working patterns
2. **Search issues:** Check [GitHub Issues](https://github.com/karlssberg/terminus/issues)
3. **Ask for help:** Open a new issue with:
   - Terminus version
   - .NET SDK version
   - Minimal reproduction code
   - Error messages
   - Generated code (if applicable)

## Next Steps

- Review [Basic Usage](basic-usage.md) guide
- Explore [Advanced Scenarios](advanced-scenarios.md)
- Check [API Reference](../../api/Terminus.html)
