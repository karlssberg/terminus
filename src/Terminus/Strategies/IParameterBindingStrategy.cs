namespace Terminus.Strategies;

public interface IParameterBindingStrategy
{
    bool CanBind(ParameterBindingContext context);
    object? BindParameter(ParameterBindingContext context);
}