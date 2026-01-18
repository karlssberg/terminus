using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Terminus.Generator.Builders;
using Terminus.Generator.Discovery;
using Terminus.Generator.Matching;

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
        ((ImmutableArray<FacadeInterfaceInfo> Facades, ImmutableArray<CandidateMethodInfo> CandidateMethods) Data, Compilation Compilation) combined)
    {
        var (facades, candidateMethods) = combined.Data;
        var compilation = combined.Compilation;

        if (candidateMethods.IsEmpty && facades.IsEmpty)
            return;

        foreach (var facade in facades)
        {
            ProcessFacade(context, facade, candidateMethods, compilation);
        }
    }

    private static void ProcessFacade(
        SourceProductionContext context,
        FacadeInterfaceInfo facade,
        ImmutableArray<CandidateMethodInfo> candidateMethods,
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

        // Step 2: Validate the matched methods
        var hasErrors = UsageValidator.Validate(context, facade, matchedMethods);

        // Skip code generation if there were errors
        if (hasErrors)
            return;

        // Step 3: Generate the facade implementation
        var generationContext = FacadeGenerationContext.Create(facade, matchedMethods);
        
        var source = FacadeBuilderOrchestrator
            .Generate(generationContext)
            .ToFullString();

        context.AddSource($"{facade.InterfaceSymbol.ToIdentifierString()}_Generated.g.cs", source);
    }
}
