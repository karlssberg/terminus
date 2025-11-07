namespace Terminus;

public interface IParameterBinder
{
    TParameter BindParameter<TParameter>(ParameterBindingContext context);
}