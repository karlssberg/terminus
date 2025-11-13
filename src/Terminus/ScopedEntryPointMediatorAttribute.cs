using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class ScopedEntryPointMediatorAttribute(params Type[] targetTypes) : Attribute, IMediatorAttribute
{
    public Type[] EntryPointAttributes { get; set; } = [typeof(EntryPointAttribute)];
    public Type[] TargetTypes { get; } = targetTypes;
}