namespace Terminus;

/// <summary>
/// Specifies how return types should be handled for aggregated methods.
/// </summary>
public enum AggregationReturnTypeStrategy
{
    /// <summary>
    /// Return collections for aggregated methods (default behavior).
    /// - T → IEnumerable&lt;T&gt;
    /// - Task&lt;T&gt; → IAsyncEnumerable&lt;T&gt;
    /// All handlers are executed and their results are returned.
    /// </summary>
    Collection = 0,

    /// <summary>
    /// Return single result from first handler for aggregated methods.
    /// - T → T (executes first handler only)
    /// - Task&lt;T&gt; → Task&lt;T&gt; (executes first handler only)
    /// Emits warning (TM0010) if multiple handlers with same signature detected.
    /// </summary>
    First = 1
}
