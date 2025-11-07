using System;
using System.Collections.Generic;
using System.Linq;
using Terminus.Strategies;

namespace Terminus;

public sealed class ParameterBindingStrategyResolver
{
    private readonly List<IParameterBindingStrategy> _strategies = [..DefaultParameterBindingStrategies.Create()];
    private readonly Dictionary<Type, IParameterBinder> _customBinders = [];

    public ParameterBindingStrategyResolver Clear()
    {
        _strategies.Clear();
        return this;
    }
    
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
    
    public ParameterBindingStrategyResolver RegisterParameterBinder<TAttribute>(IParameterBinder binder) 
        where TAttribute : ParameterBinderAttribute
    {
        _customBinders[typeof(TAttribute)] = binder;
        return this;
    }
    
    public TParameter ResolveParameter<TParameter>(string parameterName, ParameterBindingContext context)
    {
        var scopedContext = context.ForParameter(parameterName, typeof(TParameter));
        if (scopedContext.ParameterAttributeType is not null)
        {
            return _customBinders.TryGetValue(scopedContext.ParameterAttributeType, out var customBinder)
                ? customBinder.BindParameter<TParameter>(scopedContext)!
                : throw new InvalidOperationException(
                     $"No custom binder registered for attribute type '{scopedContext.ParameterAttributeType.Name}'.");

        }
        
        var strategyBinder = _strategies.FirstOrDefault(strategy => strategy.CanBind(scopedContext))
                    ?? new DependencyInjectionBindingStrategy();
        
        return (TParameter)strategyBinder.Bind(scopedContext)!;
    }
}