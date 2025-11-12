using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Terminus;

public static class TypeExtensions
{
    public static ReturnTypeKind ResolveReturnTypeKind(this MethodInfo methodInfo)
    {
        if (methodInfo.ReturnType == typeof(void))
            return ReturnTypeKind.Void;
        if (methodInfo.ReturnType == typeof(Task) || methodInfo.ReturnType == typeof(ValueTask))
            return ReturnTypeKind.Task;
        if (methodInfo.ReturnType == typeof(Task<>) || methodInfo.ReturnType == typeof(ValueTask<>))
            return ReturnTypeKind.TaskWithResult;
        if (methodInfo.ReturnType == typeof(IAsyncEnumerable<>))
            return ReturnTypeKind.AsyncEnumerable;

        return ReturnTypeKind.Result;
    }
}