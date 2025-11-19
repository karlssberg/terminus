using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Terminus.Exceptions;

namespace Terminus;

public sealed class Dispatcher<TFacade>(
    IEntryPointRouter<TFacade> router,
    IServiceProvider serviceProvider)
{
    public void Publish(
        IReadOnlyDictionary<string, object?> arguments, 
        CancellationToken cancellationToken = default)
    {

        var descriptors = GetEntryPoints(arguments).ToList();
        
        var isValid = descriptors
            .Select(d => d.ReturnKind)
            .All(r => r is ReturnTypeKind.Result or ReturnTypeKind.Void);
        
        if (!isValid) throw CreateTypeMismatchException();

        foreach (var descriptor in descriptors)
        {
            var context = new BindingContext(arguments, descriptor.GetParameterBinders(serviceProvider));
            descriptor.Invoke(context, cancellationToken);
        }
    }

    public async Task PublishAsync(
        IReadOnlyDictionary<string, object?> arguments, 
        CancellationToken cancellationToken = default)
    {
        var descriptors = GetEntryPoints(arguments).ToList();
        
        var isValid = descriptors
            .Select(d => d.ReturnKind)
            .All(r => r is not ReturnTypeKind.AsyncEnumerable);
        
        if (!isValid) throw CreateTypeMismatchException();

        var results = descriptors
            .Select(descriptor =>
            {
                var context = new BindingContext(arguments, descriptor.GetParameterBinders(serviceProvider));
                return descriptor.Invoke(context, cancellationToken);
            });
        
        foreach (var result in results)
        {
            switch (result)
            {
                case Task task:
                    await task.ConfigureAwait(false);
                    return;
                case ValueTask valueTask:
                    await valueTask.ConfigureAwait(false);
                    return;
                default:
                    return;
            }
        }
    }

    public T Send<T>(
        IReadOnlyDictionary<string, object?> arguments, 
        CancellationToken cancellationToken = default)
    {
        var descriptor = GetEntryPoint(arguments) ??  throw new TerminusEntryPointNotFoundException();
        var context = new BindingContext(arguments, descriptor.GetParameterBinders(serviceProvider));
        var result = descriptor.Invoke(context, cancellationToken);
        return result switch
        {
            T value => value,
            _ => throw CreateTypeMismatchException()
        };
    }

    public async Task<T> SendAsync<T>(
        IReadOnlyDictionary<string, object?> arguments, 
        CancellationToken cancellationToken = default)
    {

        var descriptor = GetEntryPoint(arguments) ??  throw new TerminusEntryPointNotFoundException();
        var context = new BindingContext(arguments, descriptor.GetParameterBinders(serviceProvider));
        var result = descriptor.Invoke(context, cancellationToken);
        return result switch
        {
            T value => value,
            Task<T> task => await task.ConfigureAwait(false),
            ValueTask<T> valueTask => await valueTask.AsTask().ConfigureAwait(false),
            _ => throw CreateTypeMismatchException()
        };
    }

    public IAsyncEnumerable<T> CreateStream<T>(
        IReadOnlyDictionary<string, object?> arguments, 
        CancellationToken cancellationToken = default)
    {
        var descriptor = GetEntryPoint(arguments) ??  throw new TerminusEntryPointNotFoundException();
        var context = new BindingContext(arguments, descriptor.GetParameterBinders(serviceProvider));
        var result = descriptor.Invoke(context, cancellationToken);
        return result switch
        {
            IAsyncEnumerable<T> value => value,
            _ => throw CreateTypeMismatchException()
        };
    }

    public async Task<RouteResult> Route(
        IReadOnlyDictionary<string, object?> arguments, 
        CancellationToken cancellationToken = default)
    {
        var descriptor = GetEntryPoint(arguments);
        if (descriptor == null)
        {
            return RouteResult.NotFound;
        }
        
        var context = new BindingContext(arguments, descriptor.GetParameterBinders(serviceProvider));
        var result = descriptor.Invoke(context, cancellationToken);
        var resultType = descriptor.MethodInfo.ReturnType;
        return new RouteResult(descriptor, await GetTaskResultAsync(resultType, result).ConfigureAwait(false));
    }

    private static InvalidOperationException CreateTypeMismatchException()
    {
        return new InvalidOperationException("Mismatch between return type and expected return type.");
    }

    private IEntryPointDescriptor? GetEntryPoint(IReadOnlyDictionary<string, object?> arguments)
    {
        return GetEntryPoints(arguments).FirstOrDefault();
    }

    private IEnumerable<IEntryPointDescriptor> GetEntryPoints(IReadOnlyDictionary<string, object?> arguments)
    {  
        return serviceProvider
            .GetKeyedServices<IEntryPointDescriptor>(typeof(TFacade))
            .Where(ep => router.IsMatch(ep, arguments));
    }

    private static async Task<object?> GetTaskResultAsync(Type type, object? obj)
    {
        if (obj == null) return null;
    
        if (!typeof(Task).IsAssignableFrom(type))
            return obj;
    
        var task = (Task)obj;
    
        // Only use dynamic for Task<T>, not Task
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            // Safe to use dynamic here - we know it has a result
            dynamic dynamicTask = task;
            return await dynamicTask;
        }
    
        // Non-generic Task
        await task.ConfigureAwait(false);
        return null;
    }
}