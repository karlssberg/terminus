using Microsoft.Extensions.DependencyInjection;
using Terminus;

var services = new ServiceCollection();

// Register the underlying services
services.AddTransient<GreetingService>();
services.AddTransient<TimeService>();
services.AddTransient<CalculationService>();

// Register the generated facade using Terminus extensions
services.AddTerminusFacades();

var serviceProvider = services.BuildServiceProvider();

// Resolve the facade
var facade = serviceProvider.GetRequiredService<IAppFacade>();

// Use aggregated methods from multiple types
facade.SayHello("User");

Console.WriteLine(facade.GetCurrentTime());

Console.WriteLine($"2 + 3 = {facade.Add(2, 3)}");
Console.WriteLine($"4 * 5 = {facade.Multiply(4, 5)}");

// --- Types ---

// This attribute will be used to mark methods that should be included in the facade.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class FacadeMethodAttribute : Attribute;

// The [FacadeOf] attribute tells Terminus to generate a facade implementation for this interface.
// It will aggregate all methods marked with [FacadeMethod] (the type passed to the attribute).
[FacadeOf(typeof(FacadeMethodAttribute))]
public partial interface IAppFacade;

public class GreetingService
{
    [FacadeMethod]
    public void SayHello(string name)
    {
        Console.WriteLine($"Hello, {name} from GreetingService!");
    }
}

public class TimeService
{
    [FacadeMethod]
    public string GetCurrentTime()
    {
        return $"The current time is {DateTime.Now.ToShortTimeString()}";
    }
}

[FacadeMethod] // Applying to class includes all public methods
public class CalculationService
{
    public int Add(int a, int b) => a + b;
    public int Multiply(int a, int b) => a * b;
}
