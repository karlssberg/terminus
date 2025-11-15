using System;

namespace Terminus;

public class RouteResult(ReturnTypeKind returnTypeKind, Type returnType, object? result)
{
    public ReturnTypeKind ReturnTypeKind { get; } = returnTypeKind;
    public Type ReturnType { get; } = returnType;

    public T GetResult<T>() => result switch
    {
        T castValue => castValue,
        _ => throw new InvalidCastException($"Cannot convert {result} to type {typeof(T).FullName}")
    };
    
    public object? GetResult() => result;
}