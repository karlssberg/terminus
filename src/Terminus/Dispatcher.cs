using System;
using System.Reflection;
using System.Threading.Tasks;
using Terminus.Attributes;

namespace Terminus;

public class Dispatcher<TEndpointAttribute>(
    IEntryPointRouter<TEndpointAttribute> router)
    : IDispatcher<TEndpointAttribute> 
    where TEndpointAttribute : EntryPointAttribute
{
    public void Publish(ParameterBindingContext context)
    {
        var result = router.GetEntryPoint(context).Invoker(context);
        switch (result)
        {
            case Task task:
                task.Wait();
                return;
            case ValueTask valueTask:
                valueTask.AsTask().Wait();
                return;
            case not null when IsAwaitable(result):
                AsTask(result).Wait();
                return;
            default:
                return;
        }
    }

    public async Task PublishAsync(ParameterBindingContext context)
    {
        var result = router.GetEntryPoint(context).Invoker(context);
        switch (result)
        {
            case Task task:
                await task;
                return;
            case ValueTask valueTask:
                await valueTask;
                return;
            case not null when IsAwaitable(result):
                await AsTask(result);
                return;
            default:
                return;
        }
    }

    public T Request<T>(ParameterBindingContext context)
    {
        var result = router.GetEntryPoint(context).Invoker(context);
        return result switch
        {
            T value => value,
            Task<T> task => task.Result,
            ValueTask<T> valueTask => valueTask.AsTask().Result,
            not null when IsAwaitable(result) => AsTask<T>(result).Result,
            _ => throw new InvalidOperationException("Mismatch between return type and expected return type.")
        };
    }
    
    public async Task<T> RequestAsync<T>(ParameterBindingContext context)
    {
        var result = router.GetEntryPoint(context).Invoker(context);
        return result switch
        {
            T value => value,
            Task<T> task => await task,
            ValueTask<T> valueTask => await valueTask.AsTask(),
            not null when IsAwaitable(result) => await AsTask<T>(result),
            _ => throw new InvalidOperationException("Mismatch between return type and expected return type.")
        };
    }
    

    private static async Task AsTask(object obj) => await (dynamic)obj;

    private static async Task<T> AsTask<T>(object obj) => await (dynamic)obj;

    private static bool IsAwaitable(object? obj)
    {
        return obj?
            .GetType()
            .GetMethod("GetAwaiter", BindingFlags.Public | BindingFlags.Instance) != null;
    }
}

public interface IEntryPointRouter<TEndpointAttribute>
    where TEndpointAttribute : EntryPointAttribute
{
    EntryPointDescriptor<TEndpointAttribute> GetEntryPoint(ParameterBindingContext context);
}