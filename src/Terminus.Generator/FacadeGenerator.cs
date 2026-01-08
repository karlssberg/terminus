using Microsoft.CodeAnalysis;
using Terminus.Generator.Discovery;
using Terminus.Generator.Pipeline;

namespace Terminus.Generator;

/// <summary>
/// Roslyn source generator that discovers and generates facade implementations.
/// Coordinates discovery of [FacadeOf] interfaces and attributed methods.
/// </summary>
[Generator]
public class FacadeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover facade interfaces marked with [FacadeOf]
        var discoveredFacades = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => FacadeInterfaceDiscovery.IsCandidateFacadeInterface(node),
                transform: static (ctx, ct) => FacadeInterfaceDiscovery.DiscoverFacadeInterface(ctx, ct))
            .Where(static m => m.HasValue)
            .Select((m, _) => m!.Value)
            .Collect();

        // Discover methods that have attributes - we'll filter later
        var discoveredMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => FacadeMethodDiscovery.IsCandidateMethod(node),
                transform: static (ctx, ct) => FacadeMethodDiscovery.DiscoverMethods(ctx, ct))
            .Where(static m => m.HasValue && !m.Value.IsEmpty)
            .SelectMany((m, _) => m!.Value)
            .Collect();

        // Combine both providers and execute generation pipeline
        var combined = discoveredFacades.Combine(discoveredMethods);

        context.RegisterSourceOutput(combined, FacadeGenerationPipeline.Execute);
    }
}
