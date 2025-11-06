using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Terminus;

public class Dispatcher<TEndpointAttribute>(
    IEntryPointRouter<TEndpointAttribute> router)
    : IDispatcher<TEndpointAttribute>, IAsyncDispatcher<TEndpointAttribute>
    where TEndpointAttribute : EntryPointAttribute
{
    public virtual void Publish(
        ParameterBindingContext context, 
        CancellationToken cancellationToken = default)
    {
        var descriptor = router.GetEntryPoint(context);
        var result = descriptor.Invoker(context, cancellationToken);
        switch (result)
        {
            case Task:
            case ValueTask:
            case not null when IsAwaitable(result):
                throw CreateInvalidAsyncOperationException();
            default:
                return;
        }
    }

    public virtual async Task PublishAsync(
        ParameterBindingContext context, 
        CancellationToken cancellationToken = default)
    {
        var result = router.GetEntryPoint(context).Invoker(context, cancellationToken);
        switch (result)
        {
            case Task task:
                await task.ConfigureAwait(false);
                return;
            case ValueTask valueTask:
                await valueTask.ConfigureAwait(false);
                return;
            case not null when IsAwaitable(result):
                await AsTask(result).ConfigureAwait(false);
                return;
            default:
                return;
        }
    }

    public virtual T Request<T>(
        ParameterBindingContext context,
        CancellationToken cancellationToken = default)
    {
        var result = router.GetEntryPoint(context).Invoker(context, cancellationToken);
        return result switch
        {
            T value => value,
            _ => throw CreateTypeMismatchException()
        };
    }

    public virtual async Task<T> RequestAsync<T>(
        ParameterBindingContext context,
        CancellationToken cancellationToken = default)
    {
        var result = router.GetEntryPoint(context).Invoker(context, cancellationToken);
        return result switch
        {
            T value => value,
            Task<T> task => await task.ConfigureAwait(false),
            ValueTask<T> valueTask => await valueTask.AsTask().ConfigureAwait(false),
            not null when IsAwaitable(result) => await AsTask<T>(result).ConfigureAwait(false),
            _ => throw CreateTypeMismatchException()
        };
    }

    private static InvalidOperationException CreateTypeMismatchException()
    {
        return new InvalidOperationException("Mismatch between return type and expected return type.");
    }

    private static InvalidOperationException CreateInvalidAsyncOperationException()
    {
        return new InvalidOperationException(
            "Calling Publish() on an async operation is not allowed. Use PublishAsync() instead.");
    }

    private static async Task AsTask(object obj) => await ((dynamic)obj).configureAwait(false);

    private static async Task<T> AsTask<T>(object obj) => await ((dynamic)obj).ConfigureAwait(false);

    private static bool IsAwaitable(object? obj)
    {
        return obj?
            .GetType()
            .GetMethod("GetAwaiter", BindingFlags.Public | BindingFlags.Instance) != null;
    }
}