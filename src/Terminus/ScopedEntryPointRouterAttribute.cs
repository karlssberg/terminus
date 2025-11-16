using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class ScopedEntryPointRouterAttribute(params Type[] targetTypes) : Attribute, IRouterAttribute
{
    public Type[] EntryPointAttributes { get; set; } = [typeof(EntryPointAttribute)];
    public Type[] TargetTypes { get; } = targetTypes;
}