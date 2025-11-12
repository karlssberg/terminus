using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Terminus;

public class ScopedDispatcher<TFacade>(
    IEntryPointRouter<TFacade> router,
    IServiceProvider serviceProvider)
    : Dispatcher<TFacade>(router, serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public override void Publish(ParameterBindingContext context, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        base.Publish(context, cancellationToken);
    }

    public override async Task PublishAsync(ParameterBindingContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await base.PublishAsync(context, cancellationToken).ConfigureAwait(false);
    }

    public override T Send<T>(ParameterBindingContext context, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        return base.Send<T>(context, cancellationToken);
    }

    public override async IAsyncEnumerable<T> CreateStream<T>(
        ParameterBindingContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await foreach (var item in base.CreateStream<T>(context, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public override async Task<T> SendAsync<T>(ParameterBindingContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        return await base.SendAsync<T>(context, cancellationToken).ConfigureAwait(false);
    }
}