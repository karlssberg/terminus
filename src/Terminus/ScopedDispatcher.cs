using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Terminus;

public class ScopedDispatcher<TEndpointAttribute>(
    IEntryPointRouter<TEndpointAttribute> router)
    : Dispatcher<TEndpointAttribute>(router)
    where TEndpointAttribute : EntryPointAttribute
{
    public override void Publish(ParameterBindingContext context, CancellationToken cancellationToken = default)
    {
        using var scope = context.ServiceProvider.CreateScope();
        var scopedContext = context with { ServiceProvider = scope.ServiceProvider };
        
        base.Publish(scopedContext, cancellationToken);
    }

    public override async Task PublishAsync(ParameterBindingContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = context.ServiceProvider.CreateAsyncScope();
        var scopedContext = context with { ServiceProvider = scope.ServiceProvider };
        
        await base.PublishAsync(scopedContext, cancellationToken).ConfigureAwait(false);
    }

    public override T Request<T>(ParameterBindingContext context, CancellationToken cancellationToken = default)
    {
        using var scope = context.ServiceProvider.CreateScope();
        var scopedContext = context with { ServiceProvider = scope.ServiceProvider };
        
        return base.Request<T>(scopedContext, cancellationToken);
    }

    public override async Task<T> RequestAsync<T>(ParameterBindingContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = context.ServiceProvider.CreateAsyncScope();
        var scopedContext = context with { ServiceProvider = scope.ServiceProvider };
        
        return await base.RequestAsync<T>(scopedContext, cancellationToken).ConfigureAwait(false);
    }
}