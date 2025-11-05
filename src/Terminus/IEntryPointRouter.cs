using Terminus.Attributes;

namespace Terminus;

public interface IEntryPointRouter<TEndpointAttribute>
    where TEndpointAttribute : EntryPointAttribute
{
    EntryPointDescriptor<TEndpointAttribute> GetEntryPoint(ParameterBindingContext context);
}
