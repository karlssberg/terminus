using System.Collections.Generic;

namespace Terminus;

public interface IEntryPointRouter<TEndpointAttribute>
    where TEndpointAttribute : EntryPointAttribute
{
    EntryPointDescriptor<TEndpointAttribute> GetEntryPoint(ParameterBindingContext context);
    
    IEnumerable<EntryPointDescriptor<TEndpointAttribute>> GetEntryPoints(ParameterBindingContext context);
}
