using System;
using System.Collections.Generic;
using Terminus.Strategies;

namespace Terminus;

public sealed class ParameterBindingStrategyCollection
{
    private readonly Stack<Type> _strategies = new(
    [
        typeof(DependencyInjectionBindingStrategy),
        typeof(ParameterNameBindingStrategy),
        typeof(CancellationTokenBindingStrategy),
    ]);

    public IEnumerable<Type> Strategies => _strategies;

    public ParameterBindingStrategyCollection Clear()
    {
        _strategies.Clear();
        return this;
    }
    
    public ParameterBindingStrategyCollection AddStrategy<TBindingStrategy>() where TBindingStrategy : IParameterBindingStrategy
    {
        _strategies.Push(typeof(TBindingStrategy));
        return this;
    }
}
