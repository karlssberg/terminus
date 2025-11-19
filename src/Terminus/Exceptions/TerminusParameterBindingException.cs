using System;

namespace Terminus.Exceptions;

public class TerminusParameterBindingException : TerminusException
{
    public TerminusParameterBindingException() : base("Failed to bind parameter") { }
    public TerminusParameterBindingException(string message) : base(message) { }
    public TerminusParameterBindingException(string message, Exception innerException) : base(message, innerException) { }
}