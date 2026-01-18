using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Terminus.Generator.Builders.Naming;

namespace Terminus.Generator.Validation;

/// <summary>
/// Validates that methods within aggregated groups have compatible return types.
/// </summary>
internal static class AggregationReturnTypeValidator
{
    /// <summary>
    /// Validates that all methods in each aggregated group have compatible return types.
    /// </summary>
    /// <param name="context">The source production context for reporting diagnostics.</param>
    /// <param name="facadeInfo">The facade information for naming strategy.</param>
    /// <param name="groups">The aggregated method groups to validate.</param>
    /// <returns>True if any errors were detected; otherwise, false.</returns>
    public static bool Validate(
        SourceProductionContext context,
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> groups)
    {
        var hasErrors = false;

        foreach (var group in groups)
        {
            // Single methods don't need validation
            if (!group.RequiresAggregation)
                continue;

            var baseMethod = group.PrimaryMethod;
            var baseCategory = GetReturnTypeCategory(baseMethod.ReturnTypeKind);

            foreach (var method in group.Methods.Skip(1))
            {
                var methodCategory = GetReturnTypeCategory(method.ReturnTypeKind);
                
                if (baseCategory == methodCategory)
                    continue;

                // Return types are incompatible
                var signature = GetMethodSignatureDescription(facadeInfo, baseMethod);
                var baseReturnType = GetReturnTypeDescription(baseMethod.ReturnTypeKind);
                var methodReturnType = GetReturnTypeDescription(method.ReturnTypeKind);

                var location = method.MethodSymbol.Locations.FirstOrDefault();
                var diagnostic = Diagnostic.Create(
                    Diagnostics.IncompatibleReturnTypesInAggregation,
                    location,
                    signature,
                    baseReturnType,
                    methodReturnType);
                context.ReportDiagnostic(diagnostic);
                hasErrors = true;
            }
        }

        return hasErrors;
    }

    /// <summary>
    /// Gets the compatibility category for a return type kind.
    /// Return types within the same category are compatible for aggregation.
    /// </summary>
    private static ReturnTypeCategory GetReturnTypeCategory(ReturnTypeKind kind) => kind switch
    {
        ReturnTypeKind.Void => ReturnTypeCategory.Void,
        ReturnTypeKind.Result => ReturnTypeCategory.SyncResult,
        ReturnTypeKind.Task or ReturnTypeKind.ValueTask => ReturnTypeCategory.AsyncCommand,
        ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult => ReturnTypeCategory.AsyncQuery,
        ReturnTypeKind.AsyncEnumerable => ReturnTypeCategory.AsyncStream,
        _ => ReturnTypeCategory.Unknown
    };

    private static string GetMethodSignatureDescription(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo method)
    {
        var methodName = MethodNamingStrategy.GetMethodName(facadeInfo, method);
        var parameters = string.Join(", ", method.MethodSymbol.Parameters
            .Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        return $"{methodName}({parameters})";
    }

    private static string GetReturnTypeDescription(ReturnTypeKind kind) => kind switch
    {
        ReturnTypeKind.Void => "void",
        ReturnTypeKind.Result => "T",
        ReturnTypeKind.Task => "Task",
        ReturnTypeKind.ValueTask => "ValueTask",
        ReturnTypeKind.TaskWithResult => "Task<T>",
        ReturnTypeKind.ValueTaskWithResult => "ValueTask<T>",
        ReturnTypeKind.AsyncEnumerable => "IAsyncEnumerable<T>",
        _ => "unknown"
    };

    private enum ReturnTypeCategory
    {
        Unknown,
        Void,
        SyncResult,
        AsyncCommand,
        AsyncQuery,
        AsyncStream
    }
}
