using Microsoft.Extensions.DependencyInjection;
using Terminus;


var services = new ServiceCollection();
services .AddTransient<MyImplementationA>();
services .AddTransient<MyImplementationB>();
services.AddTerminusFacades();

var serviceProvider = services.BuildServiceProvider();

var facade = serviceProvider.GetRequiredService<IMyInterface>();

Console.WriteLine("--- Attribute Metadata Demo ---");
var matchingMethods = facade.GetData("example");
foreach (var (attribute, asyncMethod) in matchingMethods)
{
    var result = await asyncMethod();
    Console.WriteLine($"Route: {attribute.Route}, Result: {result}");
}

public class MyTargetAttribute(string route) : Attribute
{
    public string Route { get; } = route;
}

[FacadeOf<MyTargetAttribute>(IncludeAttributeMetadata = true, CreateScope = true, AsyncQueryName = "GetData", AggregationMode = FacadeAggregationMode.AsyncQueries)]
public partial interface IMyInterface;

public class MyImplementationA
{
    [MyTarget("/do-work")]
    public Task<string> DoWork(string value)
    {
        return Task.FromResult($"do-work({value})");
    }
}
public class MyImplementationB
{

    [MyTarget("/get-data")]
    public Task<string> DoWork(string value)
    {
        return Task.FromResult($"get-data({value})");
    }
}