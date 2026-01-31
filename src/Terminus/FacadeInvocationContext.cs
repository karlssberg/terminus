using System;
using System.Collections.Generic;
using System.Reflection;

namespace Terminus;

/// <summary>
/// Provides context information about a facade method invocation.
/// </summary>
public sealed class FacadeInvocationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FacadeInvocationContext"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="method">The facade interface method being invoked.</param>
    /// <param name="arguments">The arguments passed to the method.</param>
    /// <param name="targetType">The type containing the target implementation method.</param>
    /// <param name="methodAttribute">The facade method attribute instance.</param>
    /// <param name="properties">A dictionary for passing data between interceptors.</param>
    /// <param name="returnTypeKind">The return type kind of the method being intercepted.</param>
    /// <param name="handlers">The handlers that will be invoked for this method.</param>
    /// <param name="isAggregated">Indicates whether multiple handlers are aggregated for this method.</param>
    public FacadeInvocationContext(
        IServiceProvider serviceProvider,
        MethodInfo method,
        object?[] arguments,
        Type targetType,
        Attribute methodAttribute,
        IDictionary<string, object?> properties,
        ReturnTypeKind returnTypeKind,
        IReadOnlyList<FacadeHandlerDescriptor> handlers,
        bool isAggregated)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        MethodAttribute = methodAttribute ?? throw new ArgumentNullException(nameof(methodAttribute));
        Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        ReturnTypeKind = returnTypeKind;
        Handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        IsAggregated = isAggregated;
    }

    /// <summary>
    /// Gets the service provider for resolving dependencies.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the facade interface method being invoked.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Gets the arguments passed to the method.
    /// </summary>
    public object?[] Arguments { get; }

    /// <summary>
    /// Gets the type containing the target implementation method.
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// Gets the facade method attribute instance (e.g., HandlerAttribute, FeatureAttribute).
    /// </summary>
    public Attribute MethodAttribute { get; }

    /// <summary>
    /// Gets a dictionary for passing data between interceptors in the chain.
    /// </summary>
    public IDictionary<string, object?> Properties { get; }

    /// <summary>
    /// Gets the return type kind of the method being intercepted.
    /// </summary>
    public ReturnTypeKind ReturnTypeKind { get; }

    /// <summary>
    /// Gets the handlers that will be invoked for this method.
    /// For non-aggregated methods, this contains a single handler.
    /// For aggregated methods, this contains all matched handlers.
    /// Interceptors implementing <see cref="IAggregatableInterceptor"/> can filter this collection
    /// to control which handlers execute.
    /// </summary>
    public IReadOnlyList<FacadeHandlerDescriptor> Handlers { get; }

    /// <summary>
    /// Gets a value indicating whether multiple handlers are aggregated for this method.
    /// When <c>true</c>, the method has multiple handlers that will all execute.
    /// When <c>false</c>, the method has a single handler (and aggregation is not enabled for this return type).
    /// </summary>
    public bool IsAggregated { get; }
}

/// <summary>
/// Provides strongly-typed context information about a facade method invocation.
/// </summary>
/// <typeparam name="TAttribute">The attribute type that marks facade methods.</typeparam>
public sealed class FacadeInvocationContext<TAttribute> where TAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FacadeInvocationContext{TAttribute}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="method">The facade interface method being invoked.</param>
    /// <param name="arguments">The arguments passed to the method.</param>
    /// <param name="targetType">The type containing the target implementation method.</param>
    /// <param name="methodAttribute">The facade method attribute instance.</param>
    /// <param name="properties">A dictionary for passing data between interceptors.</param>
    /// <param name="returnTypeKind">The return type kind of the method being intercepted.</param>
    /// <param name="handlers">The handlers that will be invoked for this method.</param>
    /// <param name="isAggregated">Indicates whether multiple handlers are aggregated for this method.</param>
    public FacadeInvocationContext(
        IServiceProvider serviceProvider,
        MethodInfo method,
        object?[] arguments,
        Type targetType,
        TAttribute methodAttribute,
        IDictionary<string, object?> properties,
        ReturnTypeKind returnTypeKind,
        IReadOnlyList<FacadeHandlerDescriptor> handlers,
        bool isAggregated)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        MethodAttribute = methodAttribute ?? throw new ArgumentNullException(nameof(methodAttribute));
        Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        ReturnTypeKind = returnTypeKind;
        Handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        IsAggregated = isAggregated;
    }

    /// <summary>
    /// Gets the service provider for resolving dependencies.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the facade interface method being invoked.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Gets the arguments passed to the method.
    /// </summary>
    public object?[] Arguments { get; }

    /// <summary>
    /// Gets the type containing the target implementation method.
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// Gets the strongly-typed facade method attribute instance.
    /// </summary>
    public TAttribute MethodAttribute { get; }

    /// <summary>
    /// Gets a dictionary for passing data between interceptors in the chain.
    /// </summary>
    public IDictionary<string, object?> Properties { get; }

    /// <summary>
    /// Gets the return type kind of the method being intercepted.
    /// </summary>
    public ReturnTypeKind ReturnTypeKind { get; }

    /// <summary>
    /// Gets the handlers that will be invoked for this method.
    /// For non-aggregated methods, this contains a single handler.
    /// For aggregated methods, this contains all matched handlers.
    /// Interceptors implementing <see cref="IAggregatableInterceptor"/> can filter this collection
    /// to control which handlers execute.
    /// </summary>
    public IReadOnlyList<FacadeHandlerDescriptor> Handlers { get; }

    /// <summary>
    /// Gets a value indicating whether multiple handlers are aggregated for this method.
    /// When <c>true</c>, the method has multiple handlers that will all execute.
    /// When <c>false</c>, the method has a single handler (and aggregation is not enabled for this return type).
    /// </summary>
    public bool IsAggregated { get; }
}
