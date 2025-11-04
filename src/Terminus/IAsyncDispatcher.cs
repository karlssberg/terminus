using System.Threading;
using System.Threading.Tasks;
using Terminus.Attributes;

namespace Terminus;

public interface IAsyncDispatcher<TEndpointAttribute> where TEndpointAttribute : EntryPointAttribute
{
    public Task PublishAsync(ParameterBindingContext context, CancellationToken cancellationToken = default);
    Task<T> RequestAsync<T>(ParameterBindingContext context, CancellationToken cancellationToken = default);
}