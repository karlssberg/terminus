namespace Terminus;

/// <summary>
/// Marks a method as an endpoint that can be wired up to infrastructure.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class EndpointAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the endpoint. If not specified, the method name is used.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets optional metadata tags for the endpoint.
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointAttribute"/> class.
    /// </summary>
    public EndpointAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointAttribute"/> class with a specific name.
    /// </summary>
    /// <param name="name">The name of the endpoint.</param>
    public EndpointAttribute(string name)
    {
        Name = name;
    }
}
