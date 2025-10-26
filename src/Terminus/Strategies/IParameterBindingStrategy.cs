namespace Terminus.Strategies;

public interface IParameterBindingStrategy
{
    bool CanBind(ParameterBindingContext context);
    object? Bind(ParameterBindingContext context);
}