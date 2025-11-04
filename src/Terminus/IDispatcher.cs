using System.Threading;
using Terminus.Attributes;

namespace Terminus;

public interface IDispatcher<TEndpointAttribute> where TEndpointAttribute : EntryPointAttribute
{
    public void Publish(ParameterBindingContext context, CancellationToken cancellationToken = default);
    T Request<T>(ParameterBindingContext context, CancellationToken cancellationToken = default);
}