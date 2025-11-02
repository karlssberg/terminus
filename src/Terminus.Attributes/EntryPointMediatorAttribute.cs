using System;

namespace Terminus.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public class EntryPointMediatorAttribute(Type? endPointAttributeType = null) : Attribute
{
    public Type ForEntryPointAttribute { get; set;  } = endPointAttributeType ?? typeof(EntryPointAttribute);
}

#if NET7_0_OR_GREATER
[AttributeUsage(AttributeTargets.Interface)]
public class EntryPointMediatorAttribute<TEndpointAttribute>
    : EntryPointMediatorAttribute(type) where TEndpointAttribute : EntryPointAttribute;
#endif