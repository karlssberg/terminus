using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Interface)]
public class AutoGenerateAttribute(params Type[] targetTypes) : Attribute
{
    public Type MethodHook { get; set; } = typeof(EntryPointAttribute);

    public Type[] TargetTypes { get; set; } = targetTypes;

    public InterfaceKind Kind { get; set; } = InterfaceKind.Mediator;
}