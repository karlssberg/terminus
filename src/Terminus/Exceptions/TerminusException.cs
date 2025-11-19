using System;

namespace Terminus.Exceptions;

public class TerminusException : Exception
{
    public TerminusException(string message) : base(message) { }
    public TerminusException(string message, Exception innerException) : base(message, innerException) { }
}