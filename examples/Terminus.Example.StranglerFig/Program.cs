using Microsoft.Extensions.DependencyInjection;
using Terminus;

// --- Strangler Fig Pattern Demo ---
// This example demonstrates how Terminus can help in a "Strangler Fig" migration.
// We have a Legacy system that we are gradually replacing with a Modern system.
// By using a Terminus-generated facade, the client code remains decoupled from 
// the underlying implementation, allowing us to move methods from Legacy to Modern
// without changing the client.

var services = new ServiceCollection();

// Initially, most logic is in the LegacyService
services.AddTransient<LegacyService>();
// As we migrate, we add logic to the ModernService
services.AddTransient<ModernService>();

// Register Terminus facades
services.AddTerminusFacades();

var serviceProvider = services.BuildServiceProvider();

// The client only interacts with ISystemFacade
var facade = serviceProvider.GetRequiredService<IMyStranglerPatternFacade>();

Console.WriteLine("--- Strangler Fig Migration Demo ---");

// 1. Calling a method that still resides in the Legacy system
Console.WriteLine("\n[Client] Requesting legacy data...");
Console.WriteLine(facade.GetLegacyData(123));

// 2. Calling a method that has already been migrated to the Modern system
// The client doesn't know it's now handled by ModernService!
Console.WriteLine("\n[Client] Processing order (migrated)...");
facade.ProcessOrder(456);

// 3. Calling another migrated method
Console.WriteLine("\n[Client] Checking status...");
Console.WriteLine($"Status: {facade.GetStatus(456)}");

Console.WriteLine("\nMigration in progress: Facade provides a stable contract while implementations shift.");

// --- Infrastructure ---

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class MyStranglerTargetAttribute : Attribute;

[FacadeOf<MyStranglerTargetAttribute>]
public partial interface IMyStranglerPatternFacade;

// --- Implementations ---

/// <summary>
/// Represents the legacy monolith that we are strangling.
/// </summary>
public class LegacyService
{
    [MyStranglerTarget]
    public string GetLegacyData(int id)
    {
        return $"[LegacyService] Returning old data format for ID {id}";
    }

    // This method used to be here, but was moved to ModernService.
    // We simply removed the [MyStranglerTarget] attribute from here and added it there.
    // public void ProcessOrder(int id) { ... }
}

/// <summary>
/// Represents the new service we are migrating towards.
/// </summary>
[MyStranglerTarget] // All methods in ModernService are included in the facade
public class ModernService
{
    // Included in facade via class-level [MyStranglerTarget] attribute
    public void ProcessOrder(int orderId)
    {
        Console.WriteLine($"[ModernService] Processing order {orderId} using the new high-performance engine.");
    }

    // Included in facade via class-level [MyStranglerTarget] attribute
    public string GetStatus(int orderId)
    {
        return $"Order {orderId} is COMPLETED (via Modern system)";
    }
}
