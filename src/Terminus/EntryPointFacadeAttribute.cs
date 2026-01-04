using System;

namespace Terminus;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class EntryPointFacadeAttribute(params Type[] entryPointAttributes) : Attribute
{
    public Type[] EntryPointAttributes { get; set; } = entryPointAttributes;
    public bool Scoped { get; set; }
    public string? CommandName { get; set; }
    public string? QueryName { get; set; }
    public string? AsyncCommandName { get; set; }
    public string? AsyncQueryName { get; set; }
}