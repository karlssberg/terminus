using System;
using System.Collections.Generic;

namespace Terminus;

public sealed record ParameterBindingContext
{
    internal ParameterBindingContext(
        string ParameterName,
        Type ParameterType, 
        IReadOnlyDictionary<string, object?> Arguments,
        IReadOnlyDictionary<string, IParameterBinder> ParameterBinders)
    {
        this.ParameterName = ParameterName;
        this.ParameterType = ParameterType;
        this.Arguments = Arguments;
        this.ParameterBinders = ParameterBinders;
    }
        
    public string ParameterName { get; }
    public Type ParameterType { get; }
    public IReadOnlyDictionary<string, object?> Arguments { get; }
    public IReadOnlyDictionary<string, IParameterBinder> ParameterBinders { get; }
    public object? Argument =>  Arguments[ParameterName];
    public IParameterBinder? GetCustomBinderOrDefault() => 
        ParameterBinders.TryGetValue(ParameterName, out var binder) 
            ? binder
            : null;
}