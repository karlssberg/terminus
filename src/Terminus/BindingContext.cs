using System;
using System.Collections.Generic;

namespace Terminus;

public sealed record BindingContext(
    IReadOnlyDictionary<string, object?> Arguments,
    IReadOnlyDictionary<string, IParameterBinder> ParameterBinders)
    : IBindingContext
{
    public IReadOnlyDictionary<string, object?> Arguments { get; } = Arguments;
    public IReadOnlyDictionary<string, IParameterBinder> ParameterBinders { get; } = ParameterBinders;

    public ParameterBindingContext ForParameter(string name, Type type) => new(name, type, Arguments,  ParameterBinders);
}  