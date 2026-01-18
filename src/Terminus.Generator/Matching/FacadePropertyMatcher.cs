using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Terminus.Generator.Matching;

/// <summary>
/// Matches candidate properties to facades based on attribute types.
/// </summary>
internal static class FacadePropertyMatcher
{
    /// <summary>
    /// Filters properties to match the facade's attribute types.
    /// </summary>
    public static ImmutableArray<CandidatePropertyInfo> MatchPropertiesToFacade(
        FacadeInterfaceInfo facade,
        ImmutableArray<CandidatePropertyInfo> candidateProperties)
    {
        // Match properties where the attribute is or inherits from the specified FacadeMethodAttributeTypes
        var matchedProperties = candidateProperties
            .Where(property => facade.FacadeMethodAttributeTypes.Any(facadeAttrType =>
                InheritsFromAttribute(property.AttributeData.AttributeClass!, facadeAttrType)))
            .ToImmutableArray();

        // Filter by TargetTypes if specified
        if (!facade.TargetTypes.IsEmpty)
        {
            matchedProperties =
            [
                ..matchedProperties
                    .Where(p => facade.TargetTypes.Any(targetType =>
                        SymbolEqualityComparer.Default.Equals(p.PropertySymbol.ContainingType, targetType)))
            ];
        }

        return matchedProperties;
    }

    private static bool InheritsFromAttribute(
        INamedTypeSymbol attributeClass,
        INamedTypeSymbol targetAttributeType)
    {
        var current = attributeClass;
        while (current != null)
        {
            // Check by symbol equality
            if (SymbolEqualityComparer.Default.Equals(current, targetAttributeType))
                return true;

            current = current.BaseType;
        }

        return false;
    }
}
