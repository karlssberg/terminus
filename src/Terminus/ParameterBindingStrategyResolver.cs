using System;
using System.Collections.Generic;
using System.Linq;
using Terminus.Strategies;

namespace Terminus;

public sealed class ParameterBindingStrategyResolver
{
    private readonly List<IParameterBindingStrategy> _strategies = [];
    private readonly Dictionary<Type, IParameterBinder> _customBinders = [];

    // Explicit builder-style API that makes ordering clear
    public ParameterBindingStrategyResolver AddStrategy(IParameterBindingStrategy strategy)
    {
        _strategies.Add(strategy);
        return this;
    }
    
    public ParameterBindingStrategyResolver InsertStrategy(int index, IParameterBindingStrategy strategy)
    {
        _strategies.Insert(index, strategy);
        return this;
    }
    
    public ParameterBindingStrategyResolver AddStrategyBefore<TStrategy>(IParameterBindingStrategy strategy) 
        where TStrategy : IParameterBindingStrategy
    {
        var index = _strategies.FindIndex(s => s is TStrategy);
        if (index == -1)
            throw new InvalidOperationException($"Strategy of type {typeof(TStrategy).Name} not found");
        
        _strategies.Insert(index, strategy);
        return this;
    }
    
    public ParameterBindingStrategyResolver AddStrategyAfter<TStrategy>(IParameterBindingStrategy strategy) 
        where TStrategy : IParameterBindingStrategy
    {
        var index = _strategies.FindIndex(s => s is TStrategy);
        if (index == -1)
            throw new InvalidOperationException($"Strategy of type {typeof(TStrategy).FullName} not found");
        
        _strategies.Insert(index + 1, strategy);
        return this;
    }
    
    public void RegisterParameterBinder<TAttribute>(IParameterBinder binder) 
        where TAttribute : ParameterBinderAttribute
    {
        _customBinders[typeof(TAttribute)] = binder;
    }
    
    public IParameterBindingStrategy GetStrategy(ParameterBindingContext context)
    {
        return _strategies.FirstOrDefault(strategy => strategy.CanBind(context))
               ?? throw new InvalidOperationException(
                    $"No binding strategy found for parameter type '{context.ParameterType.FullName}'.");
    }
    
    public IParameterBinder GetParameterBinder(Type attributeType)
    {
        if (_customBinders.TryGetValue(attributeType, out var binder))
        {
            return binder;
        }
        
        throw new InvalidOperationException(
            $"No custom binder registered for attribute type '{attributeType.Name}'.");
    }
}