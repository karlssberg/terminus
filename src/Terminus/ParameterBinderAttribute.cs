using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Parameter)]
public abstract class ParameterBinderAttribute(Type binderType) : Attribute
{
    public Type BinderType { get; } = binderType;
}