using Terminus.Exceptions;

namespace Terminus.Strategies;

public static class BindingExtensions
{
    extension(IParameterBindingStrategy bindingStrategy)
    {
        public T? Bind<T>(ParameterBindingContext context)
        {
            var boundValue = bindingStrategy.BindParameter(context);
            return boundValue switch
            {
                T value => value,
                null => default,
                _ => throw new ParameterBindingException($"Cannot bind {boundValue.GetType().Name} to type {typeof(T).Name}")
            };
        }
    }
}