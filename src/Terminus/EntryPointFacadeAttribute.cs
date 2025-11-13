using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class EntryPointFacadeAttribute(params Type[] targetTypes) : Attribute, IFacadeAttribute
{
    public Type[] EntryPointAttributes { get; set; } = [typeof(EntryPointAttribute)];
    public Type[] TargetTypes { get; } = targetTypes;
}