using System;

namespace Terminus.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public class EntryPointMediatorAttribute(Type? entryPointAttributeType = null) : Attribute
{
    public Type ForEntryPointAttribute { get; set;  } = entryPointAttributeType ?? typeof(EntryPointAttribute);
}

#if NET7_0_OR_GREATER
[AttributeUsage(AttributeTargets.Interface)]
public class EntryPointMediatorAttribute<TEntryPointAttribute>
    : EntryPointMediatorAttribute(type) where TEntryPointAttribute : EntryPointAttribute;
#endif