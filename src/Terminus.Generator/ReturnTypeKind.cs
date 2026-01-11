namespace Terminus.Generator;

public enum ReturnTypeKind
 {
     Void,
     Result,
     Task,
     TaskWithResult,
     ValueTask,
     ValueTaskWithResult,
     AsyncEnumerable,
 }