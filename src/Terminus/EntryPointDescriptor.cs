using System;
using System.Reflection;

namespace Terminus;

public class EntryPointDescriptor
{
    public MethodInfo MethodInfo { get; }
    public Func<object, ParameterBindingContext, object?> Invoker { get; }

    public EntryPointDescriptor(MethodInfo methodInfo, Func<object, ParameterBindingContext, object?> func)
    {
        MethodInfo = methodInfo;
        Invoker = func;
    }
    public EntryPointDescriptor(MethodInfo methodInfo, Action<object, ParameterBindingContext> action)
    {
        MethodInfo = methodInfo;
        Invoker = (instance, context) =>
        {
            action(instance, context);
            return null;
        };
    }
}