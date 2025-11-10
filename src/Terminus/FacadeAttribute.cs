using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class FacadeAttribute(params Type[] targetTypes) : Attribute
{
    public Type[] EntryPointAttributes { get; set; } = [typeof(EntryPointAttribute)];
    public Type[] TargetTypes { get; } = targetTypes;
}