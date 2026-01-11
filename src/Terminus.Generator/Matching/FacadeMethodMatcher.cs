using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Terminus.Generator.Matching;

/// <summary>
/// Matches candidate methods to facade interfaces based on attribute inheritance and target types.
/// </summary>
internal static class FacadeMethodMatcher
{
    /// <summary>
    /// Filters methods to match the facade's attribute types and target types.
    /// </summary>
    public static ImmutableArray<CandidateMethodInfo> MatchMethodsToFacade(
        FacadeInterfaceInfo facade,
        ImmutableArray<CandidateMethodInfo> candidateMethods)
    {
        // Match methods where the attribute is or inherits from the specified FacadeMethodAttributeTypes
        var matchedMethods = candidateMethods
            .Where(method => facade.FacadeMethodAttributeTypes.Any(facadeAttrType =>
                InheritsFromAttribute(method.AttributeData.AttributeClass!, facadeAttrType)))
            .ToImmutableArray();

        // Filter by TargetTypes if specified
        if (!facade.TargetTypes.IsEmpty)
        {
            matchedMethods = 
            [
                ..matchedMethods
                    .Where(ep => facade.TargetTypes.Any(targetType =>
                        SymbolEqualityComparer.Default.Equals(ep.MethodSymbol.ContainingType, targetType)))
            ];
        }

        return matchedMethods;
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
