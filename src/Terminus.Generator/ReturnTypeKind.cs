namespace Terminus.Generator;

/// <summary>
/// Categorizes the return type of a method for facade generation.
/// </summary>
public enum ReturnTypeKind
 {
     /// <summary>
     /// Method returns void.
     /// </summary>
     Void,
     /// <summary>
     /// Method returns a non-Task, non-ValueTask type.
     /// </summary>
     Result,
     /// <summary>
     /// Method returns <see cref="System.Threading.Tasks.Task"/>.
     /// </summary>
     Task,
     /// <summary>
     /// Method returns <see cref="System.Threading.Tasks.Task{TResult}"/>.
     /// </summary>
     TaskWithResult,
     /// <summary>
     /// Method returns <see cref="System.Threading.Tasks.ValueTask"/>.
     /// </summary>
     ValueTask,
     /// <summary>
     /// Method returns <see cref="System.Threading.Tasks.ValueTask{TResult}"/>.
     /// </summary>
     ValueTaskWithResult,
     /// <summary>
     /// Method returns <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/>.
     /// </summary>
     AsyncEnumerable,
 }