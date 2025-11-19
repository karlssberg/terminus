using System;
using Terminus.Exceptions;

namespace Terminus.Strategies;

public sealed class DependencyInjectionBindingStrategy(IServiceProvider serviceProvider) : IParameterBindingStrategy
{
    public bool CanBind(ParameterBindingContext context)
    {
        // Can bind any type - this is the fallback
        return true;
    }
    
    public object? Bind(ParameterBindingContext context)
    {
        var service = serviceProvider.GetService(context.ParameterType);
        if (service is not null) return service;
        
        // If nullable, return null
        if (IsNullable(context.ParameterType))
        {
            return null;
        }
            
        throw new ParameterBindingException(
            $"Required parameter '{context.ParameterName}' of type '{context.ParameterType.Name}' could not be resolved. " +
            $"It was not found in the data dictionary and is not registered in the dependency injection container.");

    }
    
    private static bool IsNullable(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }
}