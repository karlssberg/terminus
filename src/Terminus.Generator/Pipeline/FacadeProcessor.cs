using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Terminus.Generator.Builders;
using Terminus.Generator.Discovery;
using Terminus.Generator.Grouping;
using Terminus.Generator.Matching;
using Terminus.Generator.Validation;

namespace Terminus.Generator.Pipeline;

/// <summary>
/// Processes a single facade interface and generates its implementation.
/// </summary>
internal sealed class FacadeProcessor(
    SourceProductionContext context,
    FacadeInterfaceInfo facade,
    ImmutableArray<CandidateMethodInfo> candidateMethods,
    ImmutableArray<CandidatePropertyInfo> candidateProperties,
    Compilation compilation)
{
    public void Process()
    {
        // 1. Discovery & Filtering
        var allCandidateMethods = DiscoverAndFilterMethods();

        // 2. Matching
        var matchedMethods = MatchMethods(allCandidateMethods);
        var matchedProperties = MatchProperties();

        // 3. Validation
        if (!Validate(matchedMethods, matchedProperties))
            return;

        // 4. Grouping & Aggregation Validation
        var methodGroups = GroupAndValidateAggregation(matchedMethods);
        if (methodGroups == null)
            return;

        // 5. Generation
        Generate(methodGroups.Value, matchedProperties);
    }

    private ImmutableArray<CandidateMethodInfo> DiscoverAndFilterMethods()
    {
        var allCandidateMethods = candidateMethods;

        // Discover methods from referenced assemblies
        var discoveryMode = facade.Features.MethodDiscovery;
        if (discoveryMode != MethodDiscoveryMode.None)
        {
            var referencedMethods = ReferencedAssemblyDiscovery.DiscoverMethodsFromReferencedAssemblies(
                compilation,
                facade.FacadeMethodAttributeTypes,
                discoveryMode,
                context.CancellationToken);

            allCandidateMethods = allCandidateMethods.AddRange(referencedMethods);
        }

        // Filter out methods from open generic types
        allCandidateMethods = [
            ..allCandidateMethods
                .Where(m => !IsFromOpenGenericType(m.MethodSymbol))
        ];

        // Discover methods from open generic types (closed generic instantiations)
        var openGenericMethods = OpenGenericMethodDiscovery.DiscoverClosedGenericMethods(
            compilation,
            facade.FacadeMethodAttributeTypes,
            context,
            context.CancellationToken);

        return allCandidateMethods.AddRange(openGenericMethods);
    }

    private ImmutableArray<CandidateMethodInfo> MatchMethods(ImmutableArray<CandidateMethodInfo> allCandidateMethods)
    {
        return [
            ..FacadeMethodMatcher.MatchMethodsToFacade(facade, allCandidateMethods)
                .GroupBy(m => m.MethodSymbol, SymbolEqualityComparer.Default)
                .Select(group => group.First())
        ];
    }

    private ImmutableArray<CandidatePropertyInfo> MatchProperties()
    {
        return [
            ..FacadePropertyMatcher.MatchPropertiesToFacade(facade, candidateProperties)
                .GroupBy(p => p.PropertySymbol, SymbolEqualityComparer.Default)
                .Select(group => group.First())
        ];
    }

    private bool Validate(
        ImmutableArray<CandidateMethodInfo> matchedMethods,
        ImmutableArray<CandidatePropertyInfo> matchedProperties)
    {
        var hasMethodErrors = UsageValidator.Validate(context, facade, matchedMethods, matchedProperties);
        var hasPropertyErrors = DuplicatePropertyValidator.Validate(context, matchedProperties);

        return !hasMethodErrors && !hasPropertyErrors;
    }

    private ImmutableArray<AggregatedMethodGroup>? GroupAndValidateAggregation(ImmutableArray<CandidateMethodInfo> matchedMethods)
    {
        var methodGroups = MethodSignatureGrouper.GroupBySignature(facade, matchedMethods, compilation);

        var hasAggregationErrors = AggregationReturnTypeValidator.Validate(context, facade, methodGroups);
        if (hasAggregationErrors)
            return null;

        FirstStrategyMultipleHandlersValidator.Validate(context, facade, methodGroups);

        return methodGroups;
    }

    private void Generate(
        ImmutableArray<AggregatedMethodGroup> methodGroups,
        ImmutableArray<CandidatePropertyInfo> matchedProperties)
    {
        var generationContext = FacadeGenerationContext.CreateWithGroups(facade, methodGroups, matchedProperties);

        var source = FacadeBuilderOrchestrator
            .Generate(generationContext)
            .ToFullString();

        context.AddSource($"{facade.InterfaceSymbol.ToIdentifierString()}_Generated.g.cs", source);
    }

    private static bool IsFromOpenGenericType(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (!containingType.IsGenericType || containingType.IsUnboundGenericType)
            return false;

        return containingType.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);
    }
}
