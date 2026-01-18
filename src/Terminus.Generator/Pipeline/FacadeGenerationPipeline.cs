using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Terminus.Generator.Builders;
using Terminus.Generator.Discovery;
using Terminus.Generator.Grouping;
using Terminus.Generator.Matching;
using Terminus.Generator.Validation;

namespace Terminus.Generator.Pipeline;

/// <summary>
/// Orchestrates the facade generation pipeline: matching → validation → generation.
/// </summary>
internal static class FacadeGenerationPipeline
{
    /// <summary>
    /// Executes the complete generation pipeline for all discovered facades.
    /// </summary>
    public static void Execute(
        SourceProductionContext context,
        ((ImmutableArray<FacadeInterfaceInfo> Facades, ImmutableArray<CandidateMethodInfo> CandidateMethods, ImmutableArray<CandidatePropertyInfo> CandidateProperties) Data, Compilation Compilation) combined)
    {
        var (facades, candidateMethods, candidateProperties) = combined.Data;
        var compilation = combined.Compilation;

        if (candidateMethods.IsEmpty && candidateProperties.IsEmpty && facades.IsEmpty)
            return;

        foreach (var facade in facades)
        {
            ProcessFacade(context, facade, candidateMethods, candidateProperties, compilation);
        }
    }

    private static void ProcessFacade(
        SourceProductionContext context,
        FacadeInterfaceInfo facade,
        ImmutableArray<CandidateMethodInfo> candidateMethods,
        ImmutableArray<CandidatePropertyInfo> candidateProperties,
        Compilation compilation)
    {
        // Step 0: Discover methods from referenced assemblies based on discovery mode
        var discoveryMode = facade.Features.MethodDiscovery;
        var allCandidateMethods = candidateMethods;
        if (discoveryMode != MethodDiscoveryMode.None)
        {
            var referencedMethods = ReferencedAssemblyDiscovery.DiscoverMethodsFromReferencedAssemblies(
                compilation,
                facade.FacadeMethodAttributeTypes,
                discoveryMode,
                context.CancellationToken);

            allCandidateMethods = candidateMethods.AddRange(referencedMethods);
        }

        // Step 1: Match methods to this facade
        var matchedMethods = FacadeMethodMatcher.MatchMethodsToFacade(facade, allCandidateMethods)
            .GroupBy(m => m.MethodSymbol, SymbolEqualityComparer.Default)
            .Select(group => group.First())
            .ToImmutableArray();

        // Step 1b: Match properties to this facade
        var matchedProperties = FacadePropertyMatcher.MatchPropertiesToFacade(facade, candidateProperties)
            .GroupBy(p => p.PropertySymbol, SymbolEqualityComparer.Default)
            .Select(group => group.First())
            .ToImmutableArray();

        // Step 2: Validate the matched methods (including method-property name conflicts)
        var hasMethodErrors = UsageValidator.Validate(context, facade, matchedMethods, matchedProperties);

        // Step 2b: Validate the matched properties (check for duplicate names)
        var hasPropertyErrors = DuplicatePropertyValidator.Validate(context, matchedProperties);

        // Skip code generation if there were errors
        if (hasMethodErrors || hasPropertyErrors)
            return;

        // Step 3: Group methods by signature for aggregation
        var methodGroups = MethodSignatureGrouper.GroupBySignature(facade, matchedMethods);

        // Step 3b: Validate that aggregated methods have compatible return types
        var hasAggregationErrors = AggregationReturnTypeValidator.Validate(context, facade, methodGroups);

        // Skip code generation if there were aggregation errors
        if (hasAggregationErrors)
            return;

        // Step 4: Generate the facade implementation
        var generationContext = FacadeGenerationContext.CreateWithGroups(facade, methodGroups, matchedProperties);
        
        var source = FacadeBuilderOrchestrator
            .Generate(generationContext)
            .ToFullString();

        context.AddSource($"{facade.InterfaceSymbol.ToIdentifierString()}_Generated.g.cs", source);
    }
}
