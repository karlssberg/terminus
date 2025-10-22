using Terminus;

namespace Terminus.Sample;

// Example 1: Simple Calculator Endpoint
public class CalculatorEndpoint : IEndpoint
{
    [Endpoint]
    public int Add(int a, int b)
    {
        return a + b;
    }

    [Endpoint]
    public int Subtract(int a, int b)
    {
        return a - b;
    }

    [Endpoint("Multiply")]
    public int MultiplyNumbers(int a, int b)
    {
        return a * b;
    }
}

// Example 2: User Service with Tags
public class UserServiceEndpoint : IEndpoint
{
    [Endpoint(Tags = new[] { "user", "query" })]
    public string GetUser(int userId)
    {
        return $"User {userId}: John Doe";
    }

    [Endpoint(Tags = new[] { "user", "mutation" })]
    public string CreateUser(string name, string email)
    {
        return $"Created user: {name} ({email})";
    }

    [Endpoint(Tags = new[] { "user", "mutation" })]
    public string UpdateUser(int userId, string name)
    {
        return $"Updated user {userId} with name: {name}";
    }
}

// Example 3: Async Endpoint
public class AsyncServiceEndpoint : IEndpoint
{
    [Endpoint]
    public async Task<string> FetchDataAsync(string url)
    {
        await Task.Delay(100); // Simulate async operation
        return $"Data from {url}";
    }

    [Endpoint]
    public async Task<int> ProcessAsync(int value)
    {
        await Task.Delay(50); // Simulate async processing
        return value * 2;
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Terminus Framework Sample ===\n");

        // Create a registry and discover all endpoints in this assembly
        var registry = new EndpointRegistry();
        var count = registry.RegisterEndpoints(typeof(Program).Assembly);
        
        Console.WriteLine($"Discovered and registered {count} endpoints\n");

        // Example 1: Invoke Calculator Endpoints
        Console.WriteLine("--- Example 1: Calculator Endpoints ---");
        await InvokeCalculatorEndpoints(registry);

        // Example 2: Query Endpoints by Tag
        Console.WriteLine("\n--- Example 2: Querying by Tags ---");
        QueryEndpointsByTag(registry);

        // Example 3: Invoke Async Endpoints
        Console.WriteLine("\n--- Example 3: Async Endpoints ---");
        await InvokeAsyncEndpoints(registry);

        // Example 4: List All Endpoints
        Console.WriteLine("\n--- Example 4: All Registered Endpoints ---");
        ListAllEndpoints(registry);
    }

    static Task InvokeCalculatorEndpoints(EndpointRegistry registry)
    {
        var calculator = new CalculatorEndpoint();

        var addEndpoint = registry.GetEndpoint("Add");
        var result1 = EndpointInvoker.Invoke(addEndpoint, calculator, 5, 3);
        Console.WriteLine($"Add(5, 3) = {result1}");

        var subtractEndpoint = registry.GetEndpoint("Subtract");
        var result2 = EndpointInvoker.Invoke(subtractEndpoint, calculator, 10, 4);
        Console.WriteLine($"Subtract(10, 4) = {result2}");

        var multiplyEndpoint = registry.GetEndpoint("Multiply");
        var result3 = EndpointInvoker.Invoke(multiplyEndpoint, calculator, 6, 7);
        Console.WriteLine($"Multiply(6, 7) = {result3}");
        
        return Task.CompletedTask;
    }

    static void QueryEndpointsByTag(EndpointRegistry registry)
    {
        var queryEndpoints = registry.GetEndpointsByTag("query").ToList();
        Console.WriteLine($"Found {queryEndpoints.Count} endpoint(s) with tag 'query':");
        foreach (var endpoint in queryEndpoints)
        {
            Console.WriteLine($"  - {endpoint.Name} (Type: {endpoint.EndpointType.Name})");
        }

        var mutationEndpoints = registry.GetEndpointsByTag("mutation").ToList();
        Console.WriteLine($"\nFound {mutationEndpoints.Count} endpoint(s) with tag 'mutation':");
        foreach (var endpoint in mutationEndpoints)
        {
            Console.WriteLine($"  - {endpoint.Name} (Type: {endpoint.EndpointType.Name})");
        }
    }

    static async Task InvokeAsyncEndpoints(EndpointRegistry registry)
    {
        var asyncService = new AsyncServiceEndpoint();

        var fetchEndpoint = registry.GetEndpoint("FetchDataAsync");
        var result1 = await EndpointInvoker.InvokeAsync(fetchEndpoint, asyncService, "https://api.example.com");
        Console.WriteLine($"FetchDataAsync result: {result1}");

        var processEndpoint = registry.GetEndpoint("ProcessAsync");
        var result2 = await EndpointInvoker.InvokeAsync(processEndpoint, asyncService, 42);
        Console.WriteLine($"ProcessAsync(42) = {result2}");
    }

    static void ListAllEndpoints(EndpointRegistry registry)
    {
        foreach (var endpoint in registry.Endpoints)
        {
            var tags = endpoint.Tags.Any() ? $" [Tags: {string.Join(", ", endpoint.Tags)}]" : "";
            Console.WriteLine($"  - {endpoint.Name} (Method: {endpoint.Method.Name}){tags}");
        }
    }
}
