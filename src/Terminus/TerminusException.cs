using System;

namespace Terminus;

public class TerminusException : Exception
{
    public TerminusException() : base("Terminus exception") { }
    public TerminusException(string message) : base(message) { }
    public TerminusException(string message, Exception innerException) : base(message, innerException) { }
}