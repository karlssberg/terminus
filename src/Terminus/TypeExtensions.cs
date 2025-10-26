using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

public static class ServiceProviderExtensions
{
    public static T GetRequiredService<T>(this IServiceProvider provider) =>
        provider.GetService(typeof(T)) switch
        {
            T service => service,
            _ => throw new InvalidOperationException($"Service '{typeof(T).FullName}' not found.")
        };
}