using Microsoft.Extensions.DependencyInjection;
using Terminus.Example.Basic;

// Terminus aggregates methods from multiple unrelated types into a single, cohesive facade.
// This allows you to decouple your client code from the underlying service structure.

var services = new ServiceCollection();

// 1. Register the underlying services as usual
services.AddTransient<GreetingService>();
services.AddTransient<WeatherService>();
services.AddTransient<NewsService>();

// 2. Register the generated facade using Terminus extensions
services.AddTerminusFacades();

var serviceProvider = services.BuildServiceProvider();

// 3. Resolve and use the facade
var facade = serviceProvider.GetRequiredService<Terminus.Example.Basic.IMyAppFacade>();

facade.SayHello("Developer");
Console.WriteLine((string?)facade.GetWeather());
Console.WriteLine((string?)facade.GetLatestNews());

namespace Terminus.Example.Basic
{
    /***********************************************************/

// Define an attribute to mark methods for inclusion in the facade.
    [AttributeUsage(AttributeTargets.Method)]
    public class FacadeMethodAttribute : Attribute;

// Mark the interface with [FacadeOf] to trigger source generation.
// Terminus will find all methods marked with [FacadeMethod] and implement them here.
    [FacadeOf<FacadeMethodAttribute>]
    public partial interface IMyAppFacade;

    public class GreetingService
    {
        /// <summary>
        /// Sends a friendly greeting.
        /// </summary>
        [FacadeMethod]
        public void SayHello(string name) => Console.WriteLine($"Hello, {name}!");
    }

    public class WeatherService
    {
        /// <summary>
        /// Gets the current weather conditions.
        /// </summary>
        [FacadeMethod]
        public string GetWeather() => "The weather is sunny and 22°C.";
    }

    public class NewsService
    {
        /// <summary>
        /// Retrieves the latest news headline.
        /// </summary>
        [FacadeMethod]
        public string GetLatestNews() => "Terminus simplifies facade generation!";
    }
}