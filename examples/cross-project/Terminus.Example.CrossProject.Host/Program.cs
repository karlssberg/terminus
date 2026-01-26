using Microsoft.Extensions.DependencyInjection;
using Terminus.Example.CrossProject.Dependent;

var services = new ServiceCollection();

// Register the services that the facade will delegate to
services.AddTransient<MyService>();
services.AddTransient<MyAsyncService>();
services.AddTransient<MyStreamingService>();

// Register the Terminus facade
services.AddTerminusFacades();

var serviceProvider = services.BuildServiceProvider();

// Get the facade
var facade = serviceProvider.GetRequiredService<Terminus.Example.CrossProject.Host.IMyCrossProjectFacade>();

// Use the facade
Console.WriteLine("--- Synchronous Call ---");
var data = facade.GetData(1);
Console.WriteLine($"Received: {data}");

Console.WriteLine("\n--- Asynchronous Call ---");
var asyncData = await facade.GetDataAsync(2);
Console.WriteLine($"Received: {asyncData}");

Console.WriteLine("\n--- Streaming Call ---");
await foreach (var item in facade.StreamDataAsync(3))
{
    Console.WriteLine($"Received: {item}");
}

namespace Terminus.Example.CrossProject.Host
{
    [FacadeOf<MyTargetAttribute>(MethodDiscovery = MethodDiscoveryMode.ReferencedAssemblies)]
    public partial interface IMyCrossProjectFacade;
}