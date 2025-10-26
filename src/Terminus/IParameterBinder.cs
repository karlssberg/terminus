namespace Terminus;

public interface IParameterBinder
{
    object? BindParameter(ParameterBindingContext context);
}