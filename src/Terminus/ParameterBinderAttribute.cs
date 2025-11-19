using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Parameter)]
public abstract class ParameterBinderAttribute(Type binderType) : Attribute
{
    public Type BinderType { get; } = binderType;

    private static Type Validate(Type parameterType)
    {
        var interfaceFullName = typeof(IParameterBinder).FullName;
        var interfaceType = parameterType.GetInterface(interfaceFullName!);
        return interfaceType is not null
            ? parameterType
            : throw new ArgumentException($"Parameter '{parameterType.FullName}' does not implement '{interfaceFullName}'.");
    }
}

#if NET7_0_OR_GREATER
[AttributeUsage(AttributeTargets.Parameter)]
public abstract class ParameterBinderAttribute<TBinder>()
    : ParameterBinderAttribute(typeof(TBinder)) where TBinder : IParameterBinder;
#endif