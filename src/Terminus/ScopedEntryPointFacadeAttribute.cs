using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class ScopedEntryPointFacadeAttribute(params Type[] targetTypes) : Attribute, IEntryPointFacade
{
    public Type[] EntryPointAttributes { get; set; } = [typeof(EntryPointAttribute)];
    public Type[] TargetTypes { get; } = targetTypes;
}