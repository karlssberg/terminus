using System;
using System.Collections.Generic;
using System.Reflection;
using Terminus.Attributes;

namespace Terminus;

public class EntryPointDescriptor<TEntryPointAttribute> where TEntryPointAttribute : EntryPointAttribute
{
    public MethodInfo MethodInfo { get; }
    public Func<object, ParameterBindingContext, object?> Invoker { get; }
    
    public IEnumerable<TEntryPointAttribute> Attributes { get; }

    public EntryPointDescriptor(MethodInfo methodInfo, Action<object, ParameterBindingContext> action)
    {
        MethodInfo = methodInfo;
        Invoker = (instance, context) =>
        {
            action(instance, context);
            return null;
        };
        Attributes = methodInfo.GetCustomAttributes<TEntryPointAttribute>();
    }
}