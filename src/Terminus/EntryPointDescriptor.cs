using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Terminus.Attributes;

namespace Terminus;

public class EntryPointDescriptor<TEntryPointAttribute> where TEntryPointAttribute : EntryPointAttribute
{
    public MethodInfo MethodInfo { get; }
    public Func<ParameterBindingContext, object?> Invoker { get; }

    public IEnumerable<TEntryPointAttribute> Attributes { get; }

    public EntryPointDescriptor(MethodInfo methodInfo, Action<ParameterBindingContext> action)
    {
        MethodInfo = methodInfo;
        Invoker = context =>
        {
            action(context);
            return null;
        };
        Attributes = methodInfo.GetCustomAttributes<TEntryPointAttribute>();
    }

    public EntryPointDescriptor(MethodInfo methodInfo, Func<ParameterBindingContext, object?> function)
    {
        MethodInfo = methodInfo;
        Invoker = function;
        Attributes = methodInfo.GetCustomAttributes<TEntryPointAttribute>();
    }
}