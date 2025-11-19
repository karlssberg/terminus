using System;
using Terminus.Exceptions;

namespace Terminus;

public class RouteResult
{
    private readonly IEntryPointDescriptor? _descriptor;

    private readonly object? _result;

    private RouteResult()
    {
        _result = null;
        _descriptor = null;
    }

    public RouteResult(IEntryPointDescriptor descriptor, object? result)
    {
        _result = result;
        _descriptor = descriptor;
    }

    private IEntryPointDescriptor Descriptor => _descriptor ?? throw new TerminusEntryPointNotFoundException();

    public bool IsVoid => Descriptor.ReturnKind == ReturnTypeKind.Void;

    public Type Type => Descriptor.MethodInfo.ReturnType;

    public T GetResult<T>() => _result switch
    {
        T match => match,
        _ => throw new InvalidCastException(
            $"Cannot convert '{Type.FullName}' to type '{typeof(T).FullName}'")
    };
    
    public bool EntryPointExists => _descriptor != null;

    public static RouteResult NotFound { get; } = new();
}