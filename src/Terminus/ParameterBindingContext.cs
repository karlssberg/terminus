using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Terminus;

public sealed record ParameterBindingContext
{
#if NET8_0_OR_GREATER
    [SetsRequiredMembers]
#endif
    private ParameterBindingContext(
        string ParameterName,
        Type ParameterType, 
        IReadOnlyDictionary<string, object?>? Data = null)
    {
        this.ParameterName = ParameterName;
        this.ParameterType = ParameterType;
        this.Data = Data ?? new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());
    }
    
#if NET8_0_OR_GREATER
    [SetsRequiredMembers]
#endif
    public ParameterBindingContext(
        IReadOnlyDictionary<string, object?>? Data = null)
    {
        ParameterName = "";
        ParameterType = typeof(void);
        this.Data = Data ?? new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());
    }
        
#if NET8_0_OR_GREATER
    [SetsRequiredMembers]
#endif
    public ParameterBindingContext(
        object? Data = null) : this(Data.ToDictionary())
    {
    }
    
#if NET7_0_OR_GREATER
    public ParameterBindingContext() {}

    public required string ParameterName { get; init; }
    public required Type ParameterType { get; init; }
    public required IReadOnlyDictionary<string, object?> Data { get; init; }
    
    public bool HasDefaultValue { get; init; }
    public object? DefaultValue { get; init; }

    public Type? ParameterAttributeType { get; init; }
#else
    public string ParameterName { get; set; }
    public Type ParameterType { get; set;  }
    public IReadOnlyDictionary<string, object?> Data { get; set;  }
    
    public bool HasDefaultValue { get; set; }
    public object? DefaultValue { get; set; }
    public Type? ParameterAttributeType { get; set; }
#endif

    // Generic bag for any data the host application wants to provide

    // Typed accessor helpers
    public T? GetValue<T>(string key) where T : class
    {
        return Data.TryGetValue(key, out var value) ? value as T : null;
    }
    
    public bool TryGetValue<T>(string key, out T? value) where T : class
    {
        if (Data.TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }
        value = null;
        return false;
    }
    
    // Helper for creating scoped contexts
    public ParameterBindingContext ForParameter(string name, Type type, 
        bool hasDefault = false, object? defaultValue = null)
    {
        return new ParameterBindingContext(name, type, Data)
        {
            HasDefaultValue = hasDefault,
            DefaultValue = defaultValue,
            ParameterAttributeType = ParameterAttributeType
        };
    }
}