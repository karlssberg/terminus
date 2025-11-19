using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Terminus.Exceptions;

namespace Terminus;

public class EntryPointDescriptor<TEntryPointAttribute>(
    MethodInfo methodInfo,
    Func<IBindingContext, CancellationToken, object?> function)
    : IEntryPointDescriptor
    where TEntryPointAttribute : EntryPointAttribute
{
    public IReadOnlyDictionary<string, Type> ParameterWithAttributeBinders { get; } = methodInfo
        .GetParameters()
        .Aggregate(
            new Dictionary<string, Type>(),
            (dict, parameter) =>
            {
                if (parameter.GetCustomAttribute<ParameterBinderAttribute>() is { } parameterBinderAttribute)
                {
                    dict.Add(parameter.Name ?? "", parameterBinderAttribute.BinderType);
                }

                return dict;
            });

    public MethodInfo MethodInfo { get; } = methodInfo;
    
    public IReadOnlyDictionary<string, IParameterBinder> GetParameterBinders(IServiceProvider provider)
    {
        return ParameterWithAttributeBinders
            .ToDictionary(
                kvp => kvp.Key,
                kvp => provider.GetRequiredService(kvp.Value) as IParameterBinder
                          ?? throw new TerminusException(
                              $"The parameter binder attribute '{kvp.Value.FullName}' does not implement '{typeof(IParameterBinder).FullName}'"));
    }

    public TEntryPointAttribute Attribute { get; } = methodInfo.GetCustomAttribute<TEntryPointAttribute>()!;

    public ReturnTypeKind ReturnKind { get; } = methodInfo.ResolveReturnTypeKind();

    public Type EntryPointDescriptorType { get; } = typeof(TEntryPointAttribute);

    public EntryPointDescriptor(MethodInfo methodInfo, Action<IBindingContext, CancellationToken> action) 
        : this(methodInfo, ConvertToFunc(action))
    {
    }

    public object? Invoke(IBindingContext bindingContext, CancellationToken ct)
    {
        return function(bindingContext, ct);
    }

    private static Func<IBindingContext, CancellationToken, object?> ConvertToFunc(Action<IBindingContext, CancellationToken> action)
    {
        return (context, cancellationToken) =>
        {
            action(context, cancellationToken);
            return null;
        };
    }
}