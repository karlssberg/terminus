using System;

namespace Terminus.Exceptions;

public class TerminusEntryPointNotFoundException : TerminusException
{
    public TerminusEntryPointNotFoundException() : base("Terminus entry point not found")
    {
    }

    public TerminusEntryPointNotFoundException(string message) : base(message)
    {
    }

    public TerminusEntryPointNotFoundException(string message, Exception innerException, IBindingContext? bindingContext = null) : base(message, innerException)
    {
    }
}