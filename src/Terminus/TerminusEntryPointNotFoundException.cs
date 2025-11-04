using System;
using System.Collections.Generic;

namespace Terminus;

public class TerminusEntryPointNotFoundException : TerminusException
{
    public TerminusEntryPointNotFoundException(string message) : base(message) { }
    public TerminusEntryPointNotFoundException(string message, Exception innerException) : base(message, innerException) { }   
    public TerminusEntryPointNotFoundException() : base("Terminus entry point not found") { }

    public ParameterBindingContext ParameterBindingContext { get; set; }
}