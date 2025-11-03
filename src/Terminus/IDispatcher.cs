using System.Threading.Tasks;
using Terminus.Attributes;

namespace Terminus;

public interface IDispatcher<TEndpointAttribute> where TEndpointAttribute : EntryPointAttribute
{
    public Task PublishAsync(ParameterBindingContext context);
}