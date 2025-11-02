using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terminus.Attributes;

namespace Terminus;

public static class TypeExtensions
{
    public static IEnumerable<MethodInfo> FindEntryPointMethodInfos<TAttribute>(this Type type)
        where TAttribute : EntryPointAttribute
    {
        return type
            .GetMethods()
            .Where(m => m.GetCustomAttribute<TAttribute>() != null)
            .ToArray();
    }
}