using System;

namespace Terminus.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public class AutoGenerateAttribute(params Type[] targetTypes) : Attribute
{
    public Type MethodHook { get; set; } = typeof(EntryPointAttribute);

    public Type[] TargetTypes { get; set; } = targetTypes;
}