using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Terminus.Generator.Validation;

/// <summary>
/// Validates that properties don't have duplicate names within a facade.
/// Unlike methods which can be aggregated, properties with duplicate names are errors.
/// </summary>
internal static class DuplicatePropertyValidator
{
    /// <summary>
    /// Validates properties for duplicate names and reports diagnostics.
    /// </summary>
    /// <param name="context">The source production context for reporting diagnostics.</param>
    /// <param name="properties">The candidate properties to validate.</param>
    /// <returns>True if any errors were detected; otherwise, false.</returns>
    public static bool Validate(
        SourceProductionContext context,
        ImmutableArray<CandidatePropertyInfo> properties)
    {
        var hasErrors = false;

        // Group properties by name
        var propertyGroups = properties
            .GroupBy(p => p.PropertySymbol.Name)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in propertyGroups)
        {
            foreach (var property in group)
            {
                var location = property.PropertySymbol.Locations.FirstOrDefault();
                var diagnostic = Diagnostic.Create(
                    Diagnostics.DuplicatePropertyName,
                    location,
                    property.PropertySymbol.Name);
                context.ReportDiagnostic(diagnostic);
                hasErrors = true;
            }
        }

        return hasErrors;
    }
}
