using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Terminus.Attributes;

namespace Terminus;

public class Dispatcher<TEndpointAttribute>(
    IEntryPointRouter<TEndpointAttribute> router)
    : IDispatcher<TEndpointAttribute> 
    where TEndpointAttribute : EntryPointAttribute
{
    public void Publish(ParameterBindingContext context)
    {
        using var scope = context.ServiceProvider.CreateScope();
        var scopedContext = context with { ServiceProvider = scope.ServiceProvider };
        var descriptor = router.GetEntryPoint(scopedContext);
        var result = descriptor.Invoker(scopedContext);
        switch (result)
        {
            case Task task:
            case ValueTask valueTask:
            case not null when IsAwaitable(result):
                throw CreateInvalidAsyncOperationException();
            default:
                return;
        }
    }

    public async Task PublishAsync(ParameterBindingContext context)
    {
        await using var scope = context.ServiceProvider.CreateAsyncScope();
        var scopedContext = context with { ServiceProvider = scope.ServiceProvider };
        var result = router.GetEntryPoint(scopedContext).Invoker(scopedContext);
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

    public T Request<T>(ParameterBindingContext context)
    {
        using var scope = context.ServiceProvider.CreateScope();
        var scopedContext = context with { ServiceProvider = scope.ServiceProvider };
        var result = router.GetEntryPoint(scopedContext).Invoker(scopedContext);
        return result switch
        {
            T value => value,
            _ => throw CreateTypeMismatchException()
        };
    }

    public async Task<T> RequestAsync<T>(ParameterBindingContext context)
    {
        await using var scope = context.ServiceProvider.CreateAsyncScope();
        var scopedContext = context with { ServiceProvider = scope.ServiceProvider };
        var result = router.GetEntryPoint(scopedContext).Invoker(scopedContext);
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