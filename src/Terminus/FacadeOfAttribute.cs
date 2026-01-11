using System;
using System.Linq;

namespace Terminus;

/// <summary>
/// Marks an interface as a Terminus facade, targeting methods decorated with specific attributes.
/// </summary>
/// <param name="facadeMethodAttribute">The primary attribute type used to identify methods to be included in this facade.</param>
/// <param name="facadeMethodAttributes">Additional attribute types used to identify methods to be included in this facade.</param>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class FacadeOfAttribute(Type facadeMethodAttribute, params Type[] facadeMethodAttributes) : Attribute
{
    /// <summary>
    /// Gets or sets the collection of attribute types that identify methods to be included in the facade.
    /// </summary>
    public Type[] FacadeMethodAttributes { get; set; } = BuildFacadeMethodAttributesArray(facadeMethodAttribute, facadeMethodAttributes);

    /// <summary>
    /// Gets or sets whether the facade should be registered with a scoped lifetime.
    /// When true, a new instance of the facade is created per scope (e.g., per web request).
    /// </summary>
    public bool Scoped { get; set; }

    /// <summary>
    /// Gets or sets the name of the synchronous command method in the generated facade (i.e. for methods that have a void return).
    /// </summary>
    public string? CommandName { get; set; }

    /// <summary>
    /// Gets or sets the name of the synchronous query method in the generated facade (i.e. for methods that return a non-async result.
    /// </summary>
    public string? QueryName { get; set; }

    /// <summary>
    /// Gets or sets the name of the asynchronous command method in the generated facade (i.e. for methods that return a Task or ValueTask).
    /// </summary>
    public string? AsyncCommandName { get; set; }

    /// <summary>
    /// Gets or sets the name of the asynchronous query method in the generated facade (i.e. for methords that return a Task&lt;T&gt; or ValueTask&lt;T&gt;).
    /// </summary>
    public string? AsyncQueryName { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the asynchronous stream method in the generated facade (i.e for methods that return an IAsyncEnumerable&lt;T&gt;).
    /// </summary>
    public string? AsyncStreamName { get; set; }

    private static Type[] BuildFacadeMethodAttributesArray(
        Type facadeMethodAttribute, 
        Type[] facadeMethodAttributes)
    {
        return
        [
            facadeMethodAttribute,
            ..facadeMethodAttributes
        ];
    }
}