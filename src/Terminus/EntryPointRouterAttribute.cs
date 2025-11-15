using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class EntryPointRouterAttribute(params Type[] targetTypes) : Attribute, IMediatorAttribute
{
    public Type[] EntryPointAttributes { get; set; } = [typeof(EntryPointAttribute)];
    public Type[] TargetTypes { get; } = targetTypes;
}