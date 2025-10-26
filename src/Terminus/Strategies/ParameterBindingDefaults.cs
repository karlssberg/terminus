namespace Terminus.Strategies;

public class ParameterBindingDefaults
{
    public ParameterBindingStrategyResolver CreateDefault()
    {
        var resolver = new ParameterBindingStrategyResolver();
        
        // Order is explicit and readable
        resolver
            .AddStrategy(new CancellationTokenBindingStrategy())  // Most specific
            .AddStrategy(new NamedValueBindingStrategy())         // Try named lookup
            .AddStrategy(new DependencyInjectionBindingStrategy()); // Fallback to DI
        
        return resolver;
    }
}