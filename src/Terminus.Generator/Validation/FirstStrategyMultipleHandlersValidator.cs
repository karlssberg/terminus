using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Terminus.Generator.Builders.Naming;

namespace Terminus.Generator.Validation;

/// <summary>
/// Validates aggregated method groups when using First strategy.
/// Emits a warning when multiple handlers are detected for result-returning methods,
/// as only the first handler will be executed.
/// </summary>
internal static class FirstStrategyMultipleHandlersValidator
{
    // AggregationReturnTypeStrategy enum values: Collection = 0, First = 1
    private const int FirstStrategy = 1;

    /// <summary>
    /// Validates that users are aware when multiple handlers exist for result methods
    /// but only the first will be executed due to First strategy.
    /// </summary>
    /// <param name="context">The source production context for reporting diagnostics.</param>
    /// <param name="facadeInfo">The facade information.</param>
    /// <param name="groups">The aggregated method groups to validate.</param>
    /// <returns>True if any warnings were emitted; otherwise, false.</returns>
    public static bool Validate(
        SourceProductionContext context,
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> groups)
    {
        var strategy = facadeInfo.Features.AggregationReturnTypeStrategy;
        if (strategy != FirstStrategy)
            return false;

        var hasWarnings = false;

        foreach (var group in groups)
        {
            // Single methods don't need validation
            if (!group.RequiresAggregation)
                continue;

            var returnTypeKind = group.PrimaryMethod.ReturnTypeKind;

            // Only warn for result-returning methods (T, Task<T>, ValueTask<T>)
            // Void and Task methods don't return values so the warning is less relevant
            if (returnTypeKind != ReturnTypeKind.Result &&
                returnTypeKind != ReturnTypeKind.TaskWithResult &&
                returnTypeKind != ReturnTypeKind.ValueTaskWithResult)
                continue;

            // Multiple handlers with First strategy - warn
            var methodName = MethodNamingStrategy.GetMethodName(facadeInfo, group.PrimaryMethod);
            var firstHandlerType = group.Methods[0].MethodSymbol.ContainingType.Name;

            var location = group.PrimaryMethod.MethodSymbol.Locations.FirstOrDefault();
            var diagnostic = Diagnostic.Create(
                Diagnostics.MultipleHandlersWithFirstStrategy,
                location,
                methodName,
                firstHandlerType);

            context.ReportDiagnostic(diagnostic);
            hasWarnings = true;
        }

        return hasWarnings;
    }
}
