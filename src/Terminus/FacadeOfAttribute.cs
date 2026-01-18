using System;
using System.Linq;

namespace Terminus;

/// <summary>
/// Marks an interface as a Terminus facade, targeting methods decorated with specific attributes.
/// </summary>
/// <param name="facadeMethodAttribute">The primary attribute type used to identify methods to be included in this facade.</param>
/// <param name="facadeMethodAttributes">Additional attribute types used to identify methods to be included in this facade.</param>
[AttributeUsage(AttributeTargets.Interface)]
public class FacadeOfAttribute(Type facadeMethodAttribute, params Type[] facadeMethodAttributes) : Attribute
{
    /// <summary>
    /// Gets or sets the collection of attribute types that identify methods to be included in the facade.
    /// </summary>
    public Type[] FacadeMethodAttributes { get; set; } = BuildFacadeMethodAttributesArray(facadeMethodAttribute, facadeMethodAttributes);

    /// <summary>
    /// Gets or sets the lifetime behavior for the generated facade.
    /// </summary>
    public FacadeLifetime Lifetime { get; set; } = FacadeLifetime.Transient;

    /// <summary>
    /// Gets or sets whether the facade should be registered with a scoped lifetime.
    /// When true, a new instance of the facade is created per scope (e.g., per web request).
    /// </summary>
    [Obsolete("Use Lifetime property instead. This property will be removed in a future version.")]
    public bool Scoped
    {
        get => Lifetime == FacadeLifetime.Scoped;
        set => Lifetime = value ? FacadeLifetime.Scoped : FacadeLifetime.Transient;
    }

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
    /// Gets or sets the name of the asynchronous query method in the generated facade (i.e. for methods that return a Task&lt;T&gt; or ValueTask&lt;T&gt;).
    /// </summary>
    public string? AsyncQueryName { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the asynchronous stream method in the generated facade (i.e for methods that return an IAsyncEnumerable&lt;T&gt;).
    /// </summary>
    public string? AsyncStreamName { get; set; }

    /// <summary>
    /// Gets or sets how methods should be aggregated in the generated facade interface.
    /// Default is <see cref="FacadeAggregationMode.None"/> (no aggregation, separate methods).
    /// </summary>
    public FacadeAggregationMode AggregationMode { get; set; } = FacadeAggregationMode.None;

    /// <summary>
    /// Gets or sets which assemblies should be scanned when discovering facade methods.
    /// Default is <see cref="MethodDiscoveryMode.None"/> (only methods in the current compilation are discovered).
    /// </summary>
    public MethodDiscoveryMode MethodDiscovery { get; set; } = MethodDiscoveryMode.None;

    /// <summary>
    /// Gets or sets whether to include attribute metadata with lazy execution in facade methods.
    /// When true, facade methods return IEnumerable of tuples containing the attribute instance
    /// and a delegate (Func or Action) that can be invoked to execute the handler.
    /// This enables filtering handlers based on attribute properties before execution.
    /// Works with single methods and aggregated methods alike.
    /// Default is false.
    /// </summary>
    public bool IncludeAttributeMetadata { get; set; }

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

/// <summary>
/// Generic version of <see cref="FacadeOfAttribute"/> for compile-time type safety.
/// </summary>
/// <typeparam name="T">The attribute type used to identify methods to be included in this facade.</typeparam>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class FacadeOfAttribute<T>() : FacadeOfAttribute(typeof(T))
    where T : Attribute
{
}

/// <summary>
/// Generic version of <see cref="FacadeOfAttribute"/> for compile-time type safety with 2 attribute types.
/// </summary>
/// <typeparam name="T1">The first attribute type.</typeparam>
/// <typeparam name="T2">The second attribute type.</typeparam>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class FacadeOfAttribute<T1, T2>() : FacadeOfAttribute(typeof(T1), typeof(T2))
    where T1 : Attribute
    where T2 : Attribute
{
}

/// <summary>
/// Generic version of <see cref="FacadeOfAttribute"/> for compile-time type safety with 3 attribute types.
/// </summary>
/// <typeparam name="T1">The first attribute type.</typeparam>
/// <typeparam name="T2">The second attribute type.</typeparam>
/// <typeparam name="T3">The third attribute type.</typeparam>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class FacadeOfAttribute<T1, T2, T3>() : FacadeOfAttribute(typeof(T1), typeof(T2), typeof(T3))
    where T1 : Attribute
    where T2 : Attribute
    where T3 : Attribute
{
}

/// <summary>
/// Generic version of <see cref="FacadeOfAttribute"/> for compile-time type safety with 4 attribute types.
/// </summary>
/// <typeparam name="T1">The first attribute type.</typeparam>
/// <typeparam name="T2">The second attribute type.</typeparam>
/// <typeparam name="T3">The third attribute type.</typeparam>
/// <typeparam name="T4">The fourth attribute type.</typeparam>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class FacadeOfAttribute<T1, T2, T3, T4>() : FacadeOfAttribute(typeof(T1), typeof(T2), typeof(T3), typeof(T4))
    where T1 : Attribute
    where T2 : Attribute
    where T3 : Attribute
    where T4 : Attribute
{
}

/// <summary>
/// Generic version of <see cref="FacadeOfAttribute"/> for compile-time type safety with 5 attribute types.
/// </summary>
/// <typeparam name="T1">The first attribute type.</typeparam>
/// <typeparam name="T2">The second attribute type.</typeparam>
/// <typeparam name="T3">The third attribute type.</typeparam>
/// <typeparam name="T4">The fourth attribute type.</typeparam>
/// <typeparam name="T5">The fifth attribute type.</typeparam>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class FacadeOfAttribute<T1, T2, T3, T4, T5>() : FacadeOfAttribute(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5))
    where T1 : Attribute
    where T2 : Attribute
    where T3 : Attribute
    where T4 : Attribute
    where T5 : Attribute
{
}

/// <summary>
/// Generic version of <see cref="FacadeOfAttribute"/> for compile-time type safety with 6 attribute types.
/// </summary>
/// <typeparam name="T1">The first attribute type.</typeparam>
/// <typeparam name="T2">The second attribute type.</typeparam>
/// <typeparam name="T3">The third attribute type.</typeparam>
/// <typeparam name="T4">The fourth attribute type.</typeparam>
/// <typeparam name="T5">The fifth attribute type.</typeparam>
/// <typeparam name="T6">The sixth attribute type.</typeparam>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class FacadeOfAttribute<T1, T2, T3, T4, T5, T6>() : FacadeOfAttribute(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6))
    where T1 : Attribute
    where T2 : Attribute
    where T3 : Attribute
    where T4 : Attribute
    where T5 : Attribute
    where T6 : Attribute
{
}

/// <summary>
/// Generic version of <see cref="FacadeOfAttribute"/> for compile-time type safety with 7 attribute types.
/// </summary>
/// <typeparam name="T1">The first attribute type.</typeparam>
/// <typeparam name="T2">The second attribute type.</typeparam>
/// <typeparam name="T3">The third attribute type.</typeparam>
/// <typeparam name="T4">The fourth attribute type.</typeparam>
/// <typeparam name="T5">The fifth attribute type.</typeparam>
/// <typeparam name="T6">The sixth attribute type.</typeparam>
/// <typeparam name="T7">The seventh attribute type.</typeparam>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class FacadeOfAttribute<T1, T2, T3, T4, T5, T6, T7>() : FacadeOfAttribute(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7))
    where T1 : Attribute
    where T2 : Attribute
    where T3 : Attribute
    where T4 : Attribute
    where T5 : Attribute
    where T6 : Attribute
    where T7 : Attribute
{
}

/// <summary>
/// Generic version of <see cref="FacadeOfAttribute"/> for compile-time type safety with 8 attribute types.
/// </summary>
/// <typeparam name="T1">The first attribute type.</typeparam>
/// <typeparam name="T2">The second attribute type.</typeparam>
/// <typeparam name="T3">The third attribute type.</typeparam>
/// <typeparam name="T4">The fourth attribute type.</typeparam>
/// <typeparam name="T5">The fifth attribute type.</typeparam>
/// <typeparam name="T6">The sixth attribute type.</typeparam>
/// <typeparam name="T7">The seventh attribute type.</typeparam>
/// <typeparam name="T8">The eighth attribute type.</typeparam>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class FacadeOfAttribute<T1, T2, T3, T4, T5, T6, T7, T8>() : FacadeOfAttribute(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8))
    where T1 : Attribute
    where T2 : Attribute
    where T3 : Attribute
    where T4 : Attribute
    where T5 : Attribute
    where T6 : Attribute
    where T7 : Attribute
    where T8 : Attribute
{
}