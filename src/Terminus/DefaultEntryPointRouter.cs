using System;

namespace Terminus;

public class DefaultEntryPointRouter<TFacade>(
    IServiceProvider serviceProvider) 
    : IEntryPointRouter<TFacade>
{
    public bool IsMatch(IEntryPointDescriptor ep, ParameterBindingContext context)
    {
        return ep.MethodInfo.CanInvokeWith(context.Data);
    }
}