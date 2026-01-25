using System;

namespace Terminus;

/// <summary>
/// Specifies that a class is an implementation of a Terminus facade interface.
/// This attribute is used by the registration extensions to discover and register facades.
/// </summary>
/// <param name="facadeInterfaceType">The type of the facade interface this class implements.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class FacadeImplementationAttribute(Type facadeInterfaceType) : Attribute
{
    /// <summary>
    /// Gets the type of the facade interface implemented by this class.
    /// </summary>
    public Type FacadeInterfaceType { get; } = facadeInterfaceType;
}