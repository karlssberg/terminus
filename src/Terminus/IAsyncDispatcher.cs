using System.Threading;
using System.Threading.Tasks;

namespace Terminus;

public interface IAsyncDispatcher<TEndpointAttribute> where TEndpointAttribute : EntryPointAttribute
{
    public Task PublishAsync(ParameterBindingContext context, CancellationToken cancellationToken = default);
    Task<T> RequestAsync<T>(ParameterBindingContext context, CancellationToken cancellationToken = default);
}