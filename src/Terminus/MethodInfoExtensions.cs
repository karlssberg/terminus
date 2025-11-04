using System;
using System.Collections.Generic;
using System.Reflection;

namespace Terminus;

public static class MethodInfoExtensions
{
    public static bool CanInvokeWith(this MethodInfo method, IReadOnlyDictionary<string, object?> namedArgs)
    {
        var parameters = method.GetParameters();
        
        foreach (var param in parameters)
        {
            if (!namedArgs.TryGetValue(param.Name, out var argValue))
            {
                // If parameter is required (no default value), we can't invoke
                if (!param.IsOptional)
                {
                    return false;
                }
                
                // Optional parameter without provided value is OK
                continue;
            }

            if (argValue == null)
            {
                // Null is only valid for nullable types or reference types
                if (param.ParameterType.IsValueType && 
                    Nullable.GetUnderlyingType(param.ParameterType) == null)
                {
                    return false; // Can't pass null to non-nullable value type
                }
                continue;
            }
            
            // Check (polymorphic) type compatibility
            if (!param.ParameterType.IsInstanceOfType(argValue))
            {
                return false;
            }
        }
        
        return true;
    }
    
    public static object? InvokeWith(this MethodInfo method, object? instance, 
        Dictionary<string, object?> namedArgs)
    {
        if (!method.CanInvokeWith(namedArgs))
        {
            throw new ArgumentException("Cannot invoke method with provided arguments");
        }
        
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];
        
        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            if (namedArgs.TryGetValue(param.Name, out var arg))
            {
                args[i] = arg;
            }
            else if (param.IsOptional)
            {
                args[i] = param.DefaultValue;
            }
        }
        
        return method.Invoke(instance, args);
    }
}