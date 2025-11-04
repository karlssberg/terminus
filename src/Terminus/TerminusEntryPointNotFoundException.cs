using System;
using System.Collections.Generic;

namespace Terminus;

public class TerminusEntryPointNotFoundException : TerminusException
{
    public TerminusEntryPointNotFoundException(string message, ParameterBindingContext parameterBindingContext) : base(message)
    {
        ParameterBindingContext = parameterBindingContext;
    }
    public TerminusEntryPointNotFoundException(string message, Exception innerException, ParameterBindingContext parameterBindingContext) : base(message, innerException)
    {
        ParameterBindingContext = parameterBindingContext;
    }   
    public TerminusEntryPointNotFoundException(ParameterBindingContext parameterBindingContext) : base("Terminus entry point not found")
    {
        ParameterBindingContext = parameterBindingContext;
    }

    public ParameterBindingContext ParameterBindingContext { get; }
}