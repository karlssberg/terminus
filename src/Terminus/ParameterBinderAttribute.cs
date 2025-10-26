using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Parameter)]
public abstract class ParameterBinderAttribute : Attribute
{
    public abstract Type BinderType { get; }
}