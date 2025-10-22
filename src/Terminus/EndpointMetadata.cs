using System.Reflection;

namespace Terminus;

/// <summary>
/// Contains metadata about a discovered endpoint method.
/// </summary>
public class EndpointMetadata
{
    /// <summary>
    /// Gets the type that contains the endpoint method.
    /// </summary>
    public Type EndpointType { get; }

    /// <summary>
    /// Gets the method that defines the endpoint.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Gets the name of the endpoint.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the tags associated with the endpoint.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets the endpoint attribute.
    /// </summary>
    public EndpointAttribute Attribute { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointMetadata"/> class.
    /// </summary>
    /// <param name="endpointType">The type that contains the endpoint method.</param>
    /// <param name="method">The method that defines the endpoint.</param>
    /// <param name="attribute">The endpoint attribute.</param>
    public EndpointMetadata(Type endpointType, MethodInfo method, EndpointAttribute attribute)
    {
        EndpointType = endpointType ?? throw new ArgumentNullException(nameof(endpointType));
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        Name = attribute.Name ?? method.Name;
        Tags = attribute.Tags?.ToList() ?? new List<string>();
    }
}
