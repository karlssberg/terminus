using System;
using System.Collections.Generic;
using Terminus.Exceptions;

namespace Terminus.Strategies;

public sealed class ParameterNameBindingStrategy : IParameterBindingStrategy
{
    private static readonly HashSet<Type> SimpleTypes =
    [
        typeof(string),
        typeof(int), typeof(long), typeof(short), typeof(byte),
        typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte),
        typeof(float), typeof(double), typeof(decimal),
        typeof(bool),
        typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan),
        typeof(Guid),
        typeof(Uri)
    ];
    
    public bool CanBind(ParameterBindingContext context)
    {
        var underlyingType = Nullable.GetUnderlyingType(context.ParameterType) ?? context.ParameterType;
        return SimpleTypes.Contains(underlyingType) || underlyingType.IsEnum;
    }
    
    public object? Bind(ParameterBindingContext context)
    {
        // Look for parameter by name in the generic data bag
        if (context.Arguments.TryGetValue(context.ParameterName, out var value))
        {
            return ConvertValue(value, context.ParameterType);
        }
        
        // Check if nullable
        if (IsNullable(context.ParameterType))
        {
            return null;
        }
        
        throw new ParameterBindingException(
            $"Required parameter '{context.ParameterName}' of type '{context.ParameterType.Name}' was not found.");
    }
    
    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
        {
            return IsNullable(targetType) ? null 
                : throw new ParameterBindingException($"Cannot convert null to non-nullable type {targetType.Name}");
        }
        
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        // If already correct type, return as-is
        if (underlyingType.IsInstanceOfType(value))
        {
            return value;
        }
        
        try
        {
            if (value is not string stringValue) 
                return Convert.ChangeType(value, underlyingType);
            
            // Handle string conversion
            if (underlyingType.IsEnum)
            {
                return Enum.Parse(underlyingType, stringValue, ignoreCase: true);
            }
                
            if (underlyingType == typeof(Guid))
            {
                return Guid.Parse(stringValue);
            }
                
            if (underlyingType == typeof(Uri))
            {
                return new Uri(stringValue);
            }
                
            return Convert.ChangeType(stringValue, underlyingType);
        }
        catch (Exception ex)
        {
            throw new ParameterBindingException(
                $"Failed to convert value '{value}' to type '{targetType.Name}'.", ex);
        }
    }
    
    private static bool IsNullable(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }
}