using System;

namespace Terminus.Exceptions;

public class ParameterBindingException : Exception
{
    public ParameterBindingException(string message) : base(message) { }
    public ParameterBindingException(string message, Exception innerException) 
        : base(message, innerException) { }
}