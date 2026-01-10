using Microsoft.Extensions.DependencyInjection;
using Terminus;

// --- MediatR Alternative Demo ---
// This example shows how Terminus can be used to create a strongly-typed "Mediator" or "Bus".
// Instead of a single 'Send(object)' method that loses type safety and requires runtime lookup,
// Terminus generates a facade with explicit methods for every command and query handler.

var services = new ServiceCollection();

// 1. Register your handlers
services.AddTransient<CreateUserHandler>();
services.AddTransient<GetWeatherHandler>();

// 2. Register Terminus facades
services.AddTerminusFacades();

var serviceProvider = services.BuildServiceProvider();

// 3. Resolve the Mediator (our strongly-typed alternative to MediatR)
var mediator = serviceProvider.GetRequiredService<IMediator>();

Console.WriteLine("--- Strongly Typed Mediator (MediatR Alternative) ---");

// Commands and queries are now explicit methods on the 'mediator' interface.
// You get full IntelliSense, compile-time safety, and zero reflection at runtime.

Console.WriteLine("\n[Client] Publishing CreateUserCommand...");
mediator.Publish(new CreateUserCommand("Alice", "alice@example.com"));

Console.WriteLine("\n[Client] Calling GetWeatherQuery...");
var weather = mediator.Handle(new GetWeatherQuery("London"));
Console.WriteLine($"Result: {weather}");

// --- Infrastructure ---

// Define an attribute to mark handler methods.
[AttributeUsage(AttributeTargets.Method)]
public class HandlerAttribute : Attribute;

// The facade interface represents our "Mediator".
// We use 'CommandName' to rename methods that return 'void' (Commands) to 'Publish'.
[FacadeOf(typeof(HandlerAttribute), CommandName = "Publish")]
public partial interface IMediator;

// --- Commands & Queries ---

public record CreateUserCommand(string Name, string Email);
public record GetWeatherQuery(string City);

// --- Handlers ---

public class CreateUserHandler
{
    [Handler]
    public void Handle(CreateUserCommand command)
    {
        Console.WriteLine($"[Handler] User '{command.Name}' created with email '{command.Email}'.");
    }
}

public class GetWeatherHandler
{
    [Handler]
    public string Handle(GetWeatherQuery query)
    {
        return $"The weather in {query.City} is cloudy.";
    }
}
