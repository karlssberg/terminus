using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Threading;

namespace Terminus;

public sealed record ParameterBindingContext
{
#if NET8_0_OR_GREATER
    [SetsRequiredMembers]
#endif
    private ParameterBindingContext(
        string parameterName,
        Type parameterType, 
        IServiceProvider serviceProvider,
        IReadOnlyDictionary<string, object?>? data = null,
        CancellationToken cancellationToken = default)
    {
        ParameterName = parameterName;
        ParameterType = parameterType;
        ServiceProvider = serviceProvider;
        Data = data ?? new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());
        CancellationToken = cancellationToken;
    }
    
#if NET8_0_OR_GREATER
    [SetsRequiredMembers]
#endif
    public ParameterBindingContext(
        IServiceProvider serviceProvider,
        IReadOnlyDictionary<string, object?>? data = null,
        CancellationToken cancellationToken = default)
    {
        ParameterName = "";
        ParameterType = typeof(void);
        ServiceProvider = serviceProvider;
        Data = data ?? new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());
        CancellationToken = cancellationToken;
    }
    
#if NET7_0_OR_GREATER
    public ParameterBindingContext() {}

    public required string ParameterName { get; init; }
    public required Type ParameterType { get; init; }
    public required IServiceProvider ServiceProvider { get; init; }
    public required IReadOnlyDictionary<string, object?> Data { get; init; }
    
    public bool HasDefaultValue { get; init; }
    public object? DefaultValue { get; init; }

    public CancellationToken CancellationToken { get; init; }
    public Type? ParameterAttributeType { get; init; }
#else
    public string ParameterName { get; set; }
    public Type ParameterType { get; set;  }
    public IServiceProvider ServiceProvider { get; set; }
    public IReadOnlyDictionary<string, object?> Data { get; set;  }
    
    public bool HasDefaultValue { get; set; }
    public object? DefaultValue { get; set; }

    public CancellationToken CancellationToken { get; set; }
    public Type? ParameterAttributeType { get; set; }
#endif

    // Generic bag for any data the host application wants to provide

    // Typed accessor helpers
    public T? GetData<T>(string key) where T : class
    {
        return Data.TryGetValue(key, out var value) ? value as T : null;
    }
    
    public bool TryGetData<T>(string key, out T? value) where T : class
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
        return new ParameterBindingContext(name, type, ServiceProvider, Data)
        {
            HasDefaultValue = hasDefault,
            DefaultValue = defaultValue,
            ParameterAttributeType = ParameterAttributeType
        };
    }
}