using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Terminus;

public class Dispatcher<TFacade>(
    IEntryPointRouter<TFacade> router,
    IServiceProvider serviceProvider)
{
    public virtual void Publish(
        ParameterBindingContext context, 
        CancellationToken cancellationToken = default)
    {
        var descriptors = GetEntryPoints(context).ToList();
        
        var isValid = descriptors
            .Select(d => d.ReturnKind)
            .All(r => r is ReturnTypeKind.Result or ReturnTypeKind.Void);
        
        if (!isValid) throw CreateTypeMismatchException();

        foreach (var descriptor in descriptors)
            descriptor.Invoker(context, cancellationToken);
    }

    public virtual async Task PublishAsync(
        ParameterBindingContext context, 
        CancellationToken cancellationToken = default)
    {
        var descriptors = GetEntryPoints(context).ToList();
        
        var isValid = descriptors
            .Select(d => d.ReturnKind)
            .All(r => r is not ReturnTypeKind.AsyncEnumerable);
        
        if (!isValid) throw CreateTypeMismatchException();

        foreach (var descriptor in descriptors)
        {
            var result = descriptor.Invoker(context, cancellationToken);
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

    public virtual T Send<T>(
        ParameterBindingContext context,
        CancellationToken cancellationToken = default)
    {
        var result = GetEntryPoint(context).Invoker(context, cancellationToken);
        return result switch
        {
            T value => value,
            _ => throw CreateTypeMismatchException()
        };
    }

    public virtual async Task<T> SendAsync<T>(
        ParameterBindingContext context,
        CancellationToken cancellationToken = default)
    {
        var result = GetEntryPoint(context).Invoker(context, cancellationToken);
        return result switch
        {
            T value => value,
            Task<T> task => await task.ConfigureAwait(false),
            ValueTask<T> valueTask => await valueTask.AsTask().ConfigureAwait(false),
            _ => throw CreateTypeMismatchException()
        };
    }

    public virtual IAsyncEnumerable<T> CreateStream<T>(
        ParameterBindingContext context,
        CancellationToken cancellationToken = default)
    {
        var result = GetEntryPoint(context).Invoker(context, cancellationToken);
        return result switch
        {
            IAsyncEnumerable<T> value => value,
            _ => throw CreateTypeMismatchException()
        };
    }

    public virtual RouteResult Route(
        ParameterBindingContext context,
        CancellationToken cancellationToken = default)
    {
        var descriptor = GetEntryPoint(context);
        var result = descriptor.Invoker(context, cancellationToken);
        return new RouteResult(descriptor.ReturnKind, descriptor.MethodInfo.ReturnType, result);
    }

    private static InvalidOperationException CreateTypeMismatchException()
    {
        return new InvalidOperationException("Mismatch between return type and expected return type.");
    }
    
    private IEnumerable<IEntryPointDescriptor> GetEntryPoints(ParameterBindingContext context)
    {  
        return serviceProvider
            .GetKeyedServices<IEntryPointDescriptor>(typeof(TFacade))
            .Where(ep => router.IsMatch(ep, context));
    }

    private IEntryPointDescriptor GetEntryPoint(ParameterBindingContext context)
    {
        return GetEntryPoints(context).FirstOrDefault()
               ?? throw new TerminusEntryPointNotFoundException(context);
    }
}