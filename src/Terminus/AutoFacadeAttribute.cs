using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Interface)]
public class EntryPointFacadeAttribute(params Type[] targetTypes) : Attribute
{
    public Type EntryPointAttribute { get; set; } = typeof(EntryPointAttribute);

    public Type[] TargetTypes { get; set; } = targetTypes;
}