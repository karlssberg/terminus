namespace Terminus;

/// <summary>
/// Defines the kind of return type for a facade method.
/// </summary>
public enum ReturnTypeKind
{
    /// <summary>Method returns void.</summary>
    Void,

    /// <summary>Method returns a value synchronously.</summary>
    Result,

    /// <summary>Method returns Task (non-generic).</summary>
    Task,

    /// <summary>Method returns Task&lt;T&gt; or ValueTask&lt;T&gt;.</summary>
    TaskWithResult,

    /// <summary>Method returns IAsyncEnumerable&lt;T&gt;.</summary>
    AsyncEnumerable
}
