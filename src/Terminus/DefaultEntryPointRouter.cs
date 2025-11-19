using System;
using System.Collections.Generic;

namespace Terminus;

public class DefaultEntryPointRouter<TFacade>
    : IEntryPointRouter<TFacade>
{
    public bool IsMatch(IEntryPointDescriptor ep, IReadOnlyDictionary<string, object?> arguments)
    {
        return ep.CanInvokeWith(arguments);
    }
}