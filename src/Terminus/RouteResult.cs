using System;
using System.Threading.Tasks;

namespace Terminus;

public class RouteResult(ReturnTypeKind returnTypeKind, Type returnType, object? result)
{
    public ReturnTypeKind ReturnTypeKind { get; } = returnTypeKind;
    
    public Type ReturnType { get; } = returnType;

    public async Task<T?> GetResult<T>() => result switch
    {
        null when ReturnTypeKind == ReturnTypeKind.Void => default,
        Task<T> task => await task,
        T castValue => castValue,
        _ => throw new InvalidCastException($"Cannot convert {result} to type {typeof(T).FullName}")
    };
    
    public Task<object?> GetResult() => Task.FromResult(result);
}