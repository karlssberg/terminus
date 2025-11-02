using System;

namespace Terminus.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public abstract class ParameterBinderAttribute : Attribute
{
    public abstract Type BinderType { get; }
}