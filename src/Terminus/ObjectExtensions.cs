using System.Collections.Generic;
using System.Linq;

namespace Terminus;

public static class ObjectExtensions
{
    public static IReadOnlyDictionary<string, object?>? ToDictionary(this object? obj)
    {
        return obj?.GetType().GetProperties()
            .ToDictionary(p => p.Name, p => p?.GetValue(obj));
    }
}