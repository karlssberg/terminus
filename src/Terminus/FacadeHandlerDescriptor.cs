using System;

namespace Terminus;

/// <summary>
/// Describes a handler in a facade method invocation.
/// Used by interceptors to filter or reorder handlers in aggregated methods.
/// </summary>
public sealed class FacadeHandlerDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FacadeHandlerDescriptor"/> class.
    /// </summary>
    /// <param name="targetType">The type containing the handler method.</param>
    /// <param name="methodAttribute">The facade method attribute instance on this handler.</param>
    /// <param name="isStatic">Whether the handler method is static.</param>
    public FacadeHandlerDescriptor(Type targetType, Attribute methodAttribute, bool isStatic)
    {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        MethodAttribute = methodAttribute ?? throw new ArgumentNullException(nameof(methodAttribute));
        IsStatic = isStatic;
    }

    /// <summary>
    /// Gets the type containing the handler method.
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// Gets the facade method attribute instance on this handler (e.g., HandlerAttribute, FeatureAttribute).
    /// </summary>
    public Attribute MethodAttribute { get; }

    /// <summary>
    /// Gets a value indicating whether the handler method is static.
    /// </summary>
    public bool IsStatic { get; }
}
