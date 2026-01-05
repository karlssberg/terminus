using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class FacadeImplementationAttribute(Type facadeInterfaceType) : System.Attribute
{
    public Type FacadeInterfaceType { get; } = facadeInterfaceType;
}