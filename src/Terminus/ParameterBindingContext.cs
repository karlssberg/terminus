using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Terminus;

public sealed class ParameterBindingContext
{
#if NET8_0_OR_GREATER
    [SetsRequiredMembers]
#endif
    public ParameterBindingContext(
        string parameterName,
        Type parameterType, 
        IServiceProvider serviceProvider,
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken = default)
    {
        new ConcurrentDictionary<string, object?>().GetOrAdd(parameterName, _ => new object());
        ParameterName = parameterName;
        ParameterType = parameterType;
        ServiceProvider = serviceProvider;
        Data = data;
        CancellationToken = cancellationToken;
    }
    
#if NET8_0_OR_GREATER
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
    public string ParameterName { get; }
    public Type ParameterType { get; }
    public IServiceProvider ServiceProvider { get; }
    public IReadOnlyDictionary<string, object?> Data { get; }
    
    public bool HasDefaultValue { get; set; }
    public object? DefaultValue { get; set; }

    public CancellationToken CancellationToken { get; }
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
            DefaultValue = defaultValue
        };
    }
}