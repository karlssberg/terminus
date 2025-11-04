using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Terminus.Attributes;

namespace Terminus;

public class ScopedDispatcher<TEndpointAttribute>(
    IEntryPointRouter<TEndpointAttribute> router)
    : Dispatcher<TEndpointAttribute>(router)
    where TEndpointAttribute : EntryPointAttribute
{
    public override void Publish(ParameterBindingContext context)
    {
        using var scope = context.ServiceProvider.CreateScope();
        var scopedContext = context with { ServiceProvider = scope.ServiceProvider };
        
        base.Publish(scopedContext);
    }

    public override async Task PublishAsync(ParameterBindingContext context)
    {
        await using var scope = context.ServiceProvider.CreateAsyncScope();
        var scopedContext = context with { ServiceProvider = scope.ServiceProvider };
        
        await base.PublishAsync(scopedContext).ConfigureAwait(false);
    }

    public override T Request<T>(ParameterBindingContext context)
    {
        using var scope = context.ServiceProvider.CreateScope();
        var scopedContext = context with { ServiceProvider = scope.ServiceProvider };
        
        return base.Request<T>(scopedContext);
    }

    public override async Task<T> RequestAsync<T>(ParameterBindingContext context)
    {
        await using var scope = context.ServiceProvider.CreateAsyncScope();
        var scopedContext = context with { ServiceProvider = scope.ServiceProvider };
        
        return await base.RequestAsync<T>(scopedContext).ConfigureAwait(false);
    }
}