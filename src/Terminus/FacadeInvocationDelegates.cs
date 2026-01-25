using System.Collections.Generic;
using System.Threading.Tasks;

namespace Terminus;

/// <summary>
/// Delegate representing the next step in the synchronous void interceptor chain.
/// </summary>
/// <param name="handlers">
/// Optional filtered list of handler descriptors. When <c>null</c>, uses all handlers from the context.
/// Pass an explicit list to filter which handlers execute.
/// </param>
public delegate void FacadeVoidInvocationDelegate(
    IReadOnlyList<FacadeHandlerDescriptor>? handlers = null);

/// <summary>
/// Delegate representing the next step in the synchronous result interceptor chain.
/// </summary>
/// <typeparam name="TResult">The return type of the method.</typeparam>
/// <param name="handlers">
/// Optional filtered list of handler descriptors. When <c>null</c>, uses all handlers from the context.
/// Pass an explicit list to filter which handlers execute.
/// </param>
public delegate TResult FacadeInvocationDelegate<out TResult>(
    IReadOnlyList<FacadeHandlerDescriptor>? handlers = null);

/// <summary>
/// Delegate representing the next step in the asynchronous void interceptor chain (Task methods).
/// </summary>
/// <param name="handlers">
/// Optional filtered list of handler descriptors. When <c>null</c>, uses all handlers from the context.
/// Pass an explicit list to filter which handlers execute.
/// </param>
public delegate Task FacadeAsyncVoidInvocationDelegate(
    IReadOnlyList<FacadeHandlerDescriptor>? handlers = null);

/// <summary>
/// Delegate representing the next step in the asynchronous result interceptor chain (Task&lt;T&gt; methods).
/// </summary>
/// <typeparam name="TResult">The return type of the async method.</typeparam>
/// <param name="handlers">
/// Optional filtered list of handler descriptors. When <c>null</c>, uses all handlers from the context.
/// Pass an explicit list to filter which handlers execute.
/// </param>
public delegate ValueTask<TResult> FacadeAsyncInvocationDelegate<TResult>(
    IReadOnlyList<FacadeHandlerDescriptor>? handlers = null);

/// <summary>
/// Delegate representing the next step in the streaming interceptor chain.
/// </summary>
/// <typeparam name="TItem">The type of items in the stream.</typeparam>
/// <param name="handlers">
/// Optional filtered list of handler descriptors. When <c>null</c>, uses all handlers from the context.
/// Pass an explicit list to filter which handlers execute.
/// </param>
public delegate IAsyncEnumerable<TItem> FacadeStreamInvocationDelegate<out TItem>(
    IReadOnlyList<FacadeHandlerDescriptor>? handlers = null);
