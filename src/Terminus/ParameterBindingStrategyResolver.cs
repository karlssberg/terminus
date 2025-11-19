using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Terminus.Strategies;

namespace Terminus;

public sealed class ParameterBindingStrategyResolver(IServiceProvider serviceProvider)
{
    private readonly List<IParameterBindingStrategy> _strategies = [..DefaultParameterBindingStrategies.Create()];

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
    
    public TParameter ResolveParameter<TParameter>(string parameterName, IBindingContext context)
    {
        var parameterBindingContext = context.ForParameter(parameterName, typeof(TParameter));
        if (parameterBindingContext.GetCustomBinderOrDefault() is {} customBinder)
        {
            return customBinder.BindParameter<TParameter>(parameterBindingContext);
        }
        
        var strategyBinder = _strategies.FirstOrDefault(strategy => strategy.CanBind(parameterBindingContext))
                    ?? serviceProvider.GetRequiredService<DependencyInjectionBindingStrategy>();
        
        return (TParameter)strategyBinder.Bind(parameterBindingContext)!;
    }
}