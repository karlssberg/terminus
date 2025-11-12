using System;
using System.Reflection;
using System.Threading;

namespace Terminus;

public class EntryPointDescriptor<TEntryPointAttribute>(
    MethodInfo methodInfo,
    Func<ParameterBindingContext, CancellationToken, object?> function)
    : IEntryPointDescriptor
    where TEntryPointAttribute : EntryPointAttribute
{
    public MethodInfo MethodInfo { get; } = methodInfo;
    
    public ParameterInfo[] Parameters { get; } = methodInfo.GetParameters();

    public Func<ParameterBindingContext, CancellationToken, object?> Invoker { get; } = function;

    public TEntryPointAttribute Attribute { get; } = methodInfo.GetCustomAttribute<TEntryPointAttribute>()!;

    public ReturnTypeKind ReturnKind { get; } = methodInfo.ResolveReturnTypeKind();
    
    public Type EntryPointDescriptorType { get; } = typeof(TEntryPointAttribute);

    public EntryPointDescriptor(MethodInfo methodInfo, Action<ParameterBindingContext, CancellationToken> action) 
        : this(methodInfo, ConvertToFunc(action))
    {
    }

    private static Func<ParameterBindingContext, CancellationToken, object?> ConvertToFunc(Action<ParameterBindingContext, CancellationToken> action)
    {
        return (context, cancellationToken) =>
        {
            action(context, cancellationToken);
            return null;
        };
    }
}