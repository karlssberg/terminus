using Microsoft.Extensions.DependencyInjection;
using Terminus.Example.MediatrAlternative;

// --- MediatR Alternative Demo ---
// This example shows how Terminus can be used to create a strongly-typed "Mediator" or "Bus".
// Instead of a single 'Publish(object)' method that loses type safety and requires runtime lookup,
// Terminus generates a facade with explicit methods for every command and query handler.

var services = new ServiceCollection();

// 1. Register your handlers
services.AddTransient<CreateUserHandler>();
services.AddTransient<GetWeatherHandler>();

// 2. Register Terminus facades
services.AddTerminusFacades();

var serviceProvider = services.BuildServiceProvider();

// 3. Resolve the Mediator (our strongly-typed alternative to MediatR)
var mediator = serviceProvider.GetRequiredService<Terminus.Example.MediatrAlternative.IMyMediator>();

Console.WriteLine("--- Strongly Typed Mediator (MediatR Alternative) ---");

// Commands and queries are now explicit methods on the 'mediator' interface.
// You get full IntelliSense, compile-time safety, and zero reflection at runtime.

Console.WriteLine("\n[Client] Publishing CreateUserCommand...");
mediator.Publish(new CreateUserCommand("Alice", "alice@example.com"));

Console.WriteLine("\n[Client] Calling GetWeatherQuery...");
var weather = mediator.Send(new GetWeatherQuery("London"));
Console.WriteLine($"Result: {weather}");

namespace Terminus.Example.MediatrAlternative
{
    /***********************************************************/

// Define an attribute to mark handler methods.
    [AttributeUsage(AttributeTargets.Method)]
    public class MyHandlerAttribute : Attribute;

// The facade interface represents our "Mediator".
// We use 'CommandName' to rename methods that return 'void' (Commands) to 'Publish'.
    [FacadeOf<MyHandlerAttribute>(
        CommandName = "Publish",
        AsyncCommandName = "PublishAsync",
        QueryName = "Send",
        AsyncQueryName = "SendAsync",
        AsyncStreamName = "StreamAsync",
        AggregationMode = FacadeAggregationMode.Commands | FacadeAggregationMode.AsyncCommands)]
    public partial interface IMyMediator;

// --- Commands & Queries ---

    public sealed record CreateUserCommand(string Name, string Email);
    public sealed record GetWeatherQuery(string City);

// --- Handlers ---

    public class CreateUserHandler
    {
        [MyHandler]
        public void Handle(CreateUserCommand command)
        {
            Console.WriteLine($"[Handler] User '{command.Name}' created with email '{command.Email}'.");
        }
    
        [MyHandler]
        public async Task HandleAsync(CreateUserCommand command)
        {
            await Task.Delay(500); // Simulate async work
            Console.WriteLine($"[Handler] (Async) User '{command.Name}' created with email '{command.Email}'.");
        }
    }

    public class GetWeatherHandler
    {
        [MyHandler]
        public string Handle(GetWeatherQuery query)
        {
            return $"The weather in {query.City} is cloudy.";
        }

        [MyHandler]
        public async Task<string> HandleAsync(GetWeatherQuery query)
        {
            await Task.Delay(300); // Simulate async work
            return $"The weather in {query.City} is sunny.";
        }
    }
}