using System.Collections.Generic;
using System.Linq;

namespace Terminus;

public class DefaultEntryPointRouter<TEntryPointAttribute>(
    IEnumerable<EntryPointDescriptor<TEntryPointAttribute>> entryPointDescriptors) 
    : IEntryPointRouter<TEntryPointAttribute> 
    where TEntryPointAttribute : EntryPointAttribute
{
    public EntryPointDescriptor<TEntryPointAttribute> GetEntryPoint(ParameterBindingContext context)
    {
        return GetEntryPoints(context).FirstOrDefault()
               ?? throw new TerminusEntryPointNotFoundException(
                   $"Cannot find suitable '{typeof(TEntryPointAttribute).Name}' entry point descriptor",
                   context);
    }

    public IEnumerable<EntryPointDescriptor<TEntryPointAttribute>> GetEntryPoints(ParameterBindingContext context)
    {
        return entryPointDescriptors.Where(descriptor => descriptor.MethodInfo.CanInvokeWith(context.Data));
    }
}