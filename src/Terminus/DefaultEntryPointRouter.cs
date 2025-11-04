using System.Collections.Generic;
using System.Linq;
using Terminus.Attributes;

namespace Terminus;

public class DefaultEntryPointRouter<TEntryPointAttribute>(
    IEnumerable<EntryPointDescriptor<TEntryPointAttribute>> entryPointDescriptors) 
    : IEntryPointRouter<TEntryPointAttribute> 
    where TEntryPointAttribute : EntryPointAttribute
{
    
    public EntryPointDescriptor<TEntryPointAttribute> GetEntryPoint(ParameterBindingContext context)
    {
        return entryPointDescriptors.FirstOrDefault(descriptor => descriptor.MethodInfo.CanInvokeWith(context.Data))
            ?? throw new TerminusEntryPointNotFoundException($"Cannot find suitable '{typeof(TEntryPointAttribute).Name}' entry point descriptor")
            {
                ParameterBindingContext = context
            };
    }
}