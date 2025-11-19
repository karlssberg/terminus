using System;
using System.Collections.Generic;

namespace Terminus;

public interface IBindingContext
{   
    public IReadOnlyDictionary<string, object?> Arguments { get; }
    
    public IReadOnlyDictionary<string, IParameterBinder> ParameterBinders { get; }

    ParameterBindingContext ForParameter(string name, Type type);
}