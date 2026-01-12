using System;

namespace Terminus;

/// <summary>
/// Specifies which method types should be aggregated in the generated facade interface.
/// Multiple flags can be combined using bitwise OR operations.
/// </summary>
[Flags]
public enum FacadeAggregationMode
{
    /// <summary>
    /// No aggregation. Each discovered method generates a separate method in the facade (default behavior).
    /// Example: CreateUserHandler.Handle(CreateUserCommand) → IFacade.Handle(CreateUserCommand)
    /// </summary>
    None = 0,

    /// <summary>
    /// Aggregate synchronous command methods (void return type).
    /// Methods are grouped into a single generic method: void Execute&lt;TCommand&gt;(TCommand command)
    /// </summary>
    Commands = 1 << 0,

    /// <summary>
    /// Aggregate synchronous query methods (non-void, non-async return type).
    /// Methods are grouped into a single generic method: TResult Query&lt;TQuery, TResult&gt;(TQuery query)
    /// </summary>
    Queries = 1 << 1,

    /// <summary>
    /// Aggregate asynchronous command methods (Task/ValueTask return type).
    /// Methods are grouped into a single generic method: Task ExecuteAsync&lt;TCommand&gt;(TCommand command, CancellationToken ct)
    /// </summary>
    AsyncCommands = 1 << 2,

    /// <summary>
    /// Aggregate asynchronous query methods (Task&lt;T&gt;/ValueTask&lt;T&gt; return type).
    /// Methods are grouped into a single generic method: Task&lt;TResult&gt; QueryAsync&lt;TQuery, TResult&gt;(TQuery query, CancellationToken ct)
    /// </summary>
    AsyncQueries = 1 << 3,

    /// <summary>
    /// Aggregate asynchronous stream methods (IAsyncEnumerable&lt;T&gt; return type).
    /// Methods are grouped into a single generic method: IAsyncEnumerable&lt;TResult&gt; StreamAsync&lt;TQuery, TResult&gt;(TQuery query, CancellationToken ct)
    /// </summary>
    AsyncStreams = 1 << 4,

    /// <summary>
    /// Aggregate all method types. Equivalent to Commands | Queries | AsyncCommands | AsyncQueries | AsyncStreams.
    /// </summary>
    All = Commands | Queries | AsyncCommands | AsyncQueries | AsyncStreams
}
