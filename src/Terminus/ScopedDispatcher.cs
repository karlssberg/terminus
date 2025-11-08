using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Terminus;

public class ScopedDispatcher<TEndpointAttribute>(
    IEntryPointRouter<TEndpointAttribute> router,
    IServiceProvider serviceProvider)
    : Dispatcher<TEndpointAttribute>(router)
    where TEndpointAttribute : EntryPointAttribute
{
    public override void Publish(ParameterBindingContext context, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        base.Publish(context, cancellationToken);
    }

    public override async Task PublishAsync(ParameterBindingContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await base.PublishAsync(context, cancellationToken).ConfigureAwait(false);
    }

    public override T Send<T>(ParameterBindingContext context, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        return base.Send<T>(context, cancellationToken);
    }

    public override async IAsyncEnumerable<T> CreateStream<T>(
        ParameterBindingContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await foreach (var item in base.CreateStream<T>(context, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public override async Task<T> SendAsync<T>(ParameterBindingContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        return await base.SendAsync<T>(context, cancellationToken).ConfigureAwait(false);
    }
}