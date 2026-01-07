using System;
using System.Linq;

namespace Terminus;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class FacadeOfAttribute(Type facadeMethodAttribute, params Type[] facadeMethodAttributes) : Attribute
{
    public Type[] FacadeMethodAttributes { get; set; } = BuildFacadeMethodAttributesArray(facadeMethodAttribute, facadeMethodAttributes);

    public bool Scoped { get; set; }

    public string? CommandName { get; set; }

    public string? QueryName { get; set; }

    public string? AsyncCommandName { get; set; }

    public string? AsyncQueryName { get; set; }
    
    public string? AsyncStreamName { get; set; }

    private static Type[] BuildFacadeMethodAttributesArray(
        Type facadeMethodAttribute,
        Type[] facadeMethodAttributes) => Enumerable
            .Empty<Type>()
            .Append(facadeMethodAttribute)
            .Concat(facadeMethodAttributes)
            .ToArray();
}