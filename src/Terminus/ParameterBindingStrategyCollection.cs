using System;
using System.Collections.Generic;
using Terminus.Strategies;

namespace Terminus;

public sealed class EntryPointOptions
{
    private readonly Stack<Type> _strategies = new(
    [
        typeof(DependencyInjectionBindingStrategy),
        typeof(ParameterNameBindingStrategy),
        typeof(CancellationTokenBindingStrategy),
    ]);

    public IEnumerable<Type> Strategies => _strategies;

    public EntryPointOptions ClearStrategies()
    {
        _strategies.Clear();
        return this;
    }
    
    public EntryPointOptions AddStrategy<TBindingStrategy>() where TBindingStrategy : IParameterBindingStrategy
    {
        _strategies.Push(typeof(TBindingStrategy));
        return this;
    }
}
