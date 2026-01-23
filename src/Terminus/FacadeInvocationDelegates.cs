using System.Collections.Generic;
using System.Threading.Tasks;

namespace Terminus;

/// <summary>
/// Delegate representing the next step in the synchronous interceptor chain.
/// </summary>
/// <typeparam name="TResult">The return type of the method, or <see cref="object"/> for void methods.</typeparam>
public delegate TResult? FacadeInvocationDelegate<out TResult>();

/// <summary>
/// Delegate representing the next step in the asynchronous interceptor chain.
/// </summary>
/// <typeparam name="TResult">The return type of the async method, or <see cref="object"/> for Task (non-generic) methods.</typeparam>
public delegate ValueTask<TResult?> FacadeAsyncInvocationDelegate<TResult>();

/// <summary>
/// Delegate representing the next step in the streaming interceptor chain.
/// </summary>
/// <typeparam name="TItem">The type of items in the stream.</typeparam>
public delegate IAsyncEnumerable<TItem> FacadeStreamInvocationDelegate<out TItem>();
