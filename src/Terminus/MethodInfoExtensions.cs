using System;
using System.Collections.Generic;
using System.Reflection;

namespace Terminus;

public static class MethodInfoExtensions
{
    extension(IEntryPointDescriptor entryPointDescriptor)
    {
        public bool CanInvokeWith(IReadOnlyDictionary<string, object?> arguments)
        {
            foreach (var param in entryPointDescriptor.MethodInfo.GetParameters())
            {
                if (entryPointDescriptor.ParameterWithAttributeBinders.ContainsKey(param.Name!))
                {
                    continue;
                }
                
                if (!arguments.TryGetValue(param.Name!, out var argValue))
                {
                    // If parameter is required (no default value), we can't invoke
                    if (!param.IsOptional)
                    {
                        return false;
                    }
                
                    // Optional parameter without provided value is OK
                    continue;
                }

                if (argValue is null)
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
    }
}