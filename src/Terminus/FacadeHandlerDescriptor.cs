using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Terminus;

/// <summary>
/// Base class describing a handler in a facade method invocation.
/// Used by interceptors to filter or reorder handlers in aggregated methods.
/// </summary>
public abstract class FacadeHandlerDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FacadeHandlerDescriptor"/> class.
    /// </summary>
    /// <param name="targetType">The type containing the handler method.</param>
    /// <param name="methodAttribute">The facade method attribute instance on this handler.</param>
    /// <param name="isStatic">Whether the handler method is static.</param>
    protected FacadeHandlerDescriptor(Type targetType, Attribute methodAttribute, bool isStatic)
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

/// <summary>
/// Describes a void synchronous handler in a facade method invocation.
/// </summary>
public sealed class FacadeVoidHandlerDescriptor : FacadeHandlerDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FacadeVoidHandlerDescriptor"/> class.
    /// </summary>
    /// <param name="targetType">The type containing the handler method.</param>
    /// <param name="methodAttribute">The facade method attribute instance on this handler.</param>
    /// <param name="isStatic">Whether the handler method is static.</param>
    /// <param name="invoke">The delegate to invoke the handler method.</param>
    public FacadeVoidHandlerDescriptor(Type targetType, Attribute methodAttribute, bool isStatic, Action invoke)
        : base(targetType, methodAttribute, isStatic)
    {
        Invoke = invoke ?? throw new ArgumentNullException(nameof(invoke));
    }

    /// <summary>
    /// Gets the delegate to invoke the handler method.
    /// </summary>
    public Action Invoke { get; }
}

/// <summary>
/// Describes a synchronous result-returning handler in a facade method invocation.
/// </summary>
/// <typeparam name="TResult">The return type of the handler method.</typeparam>
public sealed class FacadeSyncHandlerDescriptor<TResult> : FacadeHandlerDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FacadeSyncHandlerDescriptor{TResult}"/> class.
    /// </summary>
    /// <param name="targetType">The type containing the handler method.</param>
    /// <param name="methodAttribute">The facade method attribute instance on this handler.</param>
    /// <param name="isStatic">Whether the handler method is static.</param>
    /// <param name="invoke">The delegate to invoke the handler method.</param>
    public FacadeSyncHandlerDescriptor(Type targetType, Attribute methodAttribute, bool isStatic, Func<TResult> invoke)
        : base(targetType, methodAttribute, isStatic)
    {
        Invoke = invoke ?? throw new ArgumentNullException(nameof(invoke));
    }

    /// <summary>
    /// Gets the delegate to invoke the handler method.
    /// </summary>
    public Func<TResult> Invoke { get; }
}

/// <summary>
/// Describes an asynchronous void handler (Task/ValueTask) in a facade method invocation.
/// </summary>
public sealed class FacadeAsyncVoidHandlerDescriptor : FacadeHandlerDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FacadeAsyncVoidHandlerDescriptor"/> class.
    /// </summary>
    /// <param name="targetType">The type containing the handler method.</param>
    /// <param name="methodAttribute">The facade method attribute instance on this handler.</param>
    /// <param name="isStatic">Whether the handler method is static.</param>
    /// <param name="invokeAsync">The delegate to invoke the handler method asynchronously.</param>
    public FacadeAsyncVoidHandlerDescriptor(Type targetType, Attribute methodAttribute, bool isStatic, Func<Task> invokeAsync)
        : base(targetType, methodAttribute, isStatic)
    {
        InvokeAsync = invokeAsync ?? throw new ArgumentNullException(nameof(invokeAsync));
    }

    /// <summary>
    /// Gets the delegate to invoke the handler method asynchronously.
    /// </summary>
    public Func<Task> InvokeAsync { get; }
}

/// <summary>
/// Describes an asynchronous result-returning handler (Task&lt;T&gt;/ValueTask&lt;T&gt;) in a facade method invocation.
/// </summary>
/// <typeparam name="TResult">The return type of the handler method.</typeparam>
public sealed class FacadeAsyncHandlerDescriptor<TResult> : FacadeHandlerDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FacadeAsyncHandlerDescriptor{TResult}"/> class.
    /// </summary>
    /// <param name="targetType">The type containing the handler method.</param>
    /// <param name="methodAttribute">The facade method attribute instance on this handler.</param>
    /// <param name="isStatic">Whether the handler method is static.</param>
    /// <param name="invokeAsync">The delegate to invoke the handler method asynchronously.</param>
    public FacadeAsyncHandlerDescriptor(Type targetType, Attribute methodAttribute, bool isStatic, Func<ValueTask<TResult>> invokeAsync)
        : base(targetType, methodAttribute, isStatic)
    {
        InvokeAsync = invokeAsync ?? throw new ArgumentNullException(nameof(invokeAsync));
    }

    /// <summary>
    /// Gets the delegate to invoke the handler method asynchronously.
    /// </summary>
    public Func<ValueTask<TResult>> InvokeAsync { get; }
}

/// <summary>
/// Describes a streaming handler (IAsyncEnumerable&lt;T&gt;) in a facade method invocation.
/// </summary>
/// <typeparam name="TItem">The type of items in the stream.</typeparam>
public sealed class FacadeStreamHandlerDescriptor<TItem> : FacadeHandlerDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FacadeStreamHandlerDescriptor{TItem}"/> class.
    /// </summary>
    /// <param name="targetType">The type containing the handler method.</param>
    /// <param name="methodAttribute">The facade method attribute instance on this handler.</param>
    /// <param name="isStatic">Whether the handler method is static.</param>
    /// <param name="invoke">The delegate to invoke the handler method.</param>
    public FacadeStreamHandlerDescriptor(Type targetType, Attribute methodAttribute, bool isStatic, Func<IAsyncEnumerable<TItem>> invoke)
        : base(targetType, methodAttribute, isStatic)
    {
        Invoke = invoke ?? throw new ArgumentNullException(nameof(invoke));
    }

    /// <summary>
    /// Gets the delegate to invoke the handler method.
    /// </summary>
    public Func<IAsyncEnumerable<TItem>> Invoke { get; }
}
