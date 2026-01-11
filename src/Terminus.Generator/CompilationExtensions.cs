using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

public static class CompilationExtensions
{
    public static ReturnTypeKind ResolveReturnTypeKind(this Compilation compilation, IMethodSymbol method)
    {
        var returnType = method.ReturnType;

        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var taskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        var valueTaskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        var asyncEnumerableType = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
        
        if (SymbolEqualityComparer.Default.Equals(returnType, taskType))
            return ReturnTypeKind.Task;

        if (SymbolEqualityComparer.Default.Equals(returnType, valueTaskType))
            return ReturnTypeKind.ValueTask;

        if (returnType.SpecialType == SpecialType.System_Void) 
            return ReturnTypeKind.Void;
        
        if (returnType is not INamedTypeSymbol namedType) 
            return ReturnTypeKind.Result;
        
        if (SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, taskOfTType))
            return ReturnTypeKind.TaskWithResult;

        if (SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, valueTaskOfTType))
            return ReturnTypeKind.ValueTaskWithResult;

        if (SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, asyncEnumerableType))
            return ReturnTypeKind.AsyncEnumerable;

        return ReturnTypeKind.Result;
    }
}