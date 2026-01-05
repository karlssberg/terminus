using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class FacadeOfAttribute(params Type[] facadeMethodAttributes) : Attribute
{
    public Type[] FacadeMethodAttributes { get; set; } = facadeMethodAttributes;
    public bool Scoped { get; set; }
    public string? CommandName { get; set; }
    public string? QueryName { get; set; }
    public string? AsyncCommandName { get; set; }
    public string? AsyncQueryName { get; set; }
}