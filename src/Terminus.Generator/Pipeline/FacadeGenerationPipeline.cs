using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

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

        var facadeProcessors = facades
            .Select(facade => new FacadeProcessor(context, facade, candidateMethods, candidateProperties, compilation));
        
        foreach (var processor in facadeProcessors)
        {
            processor.Process();
        }
    }
}
