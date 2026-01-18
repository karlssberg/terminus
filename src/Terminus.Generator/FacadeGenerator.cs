using Microsoft.CodeAnalysis;
using Terminus.Generator.Discovery;
using Terminus.Generator.Pipeline;

namespace Terminus.Generator;

/// <summary>
/// Roslyn source generator that discovers and generates facade implementations.
/// Coordinates discovery of [FacadeOf] interfaces and attributed methods and properties.
/// </summary>
[Generator]
public class FacadeGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the source generator by registering discovery and generation stages.
    /// </summary>
    /// <param name="context">The initialization context provided by the Roslyn compiler.</param>
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
            .Where(static m => m is { IsEmpty: false })
            .SelectMany((m, _) => m!.Value)
            .Collect();

        // Discover types (classes/structs/records) that have attributes - include all their public methods
        var discoveredTypeMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => FacadeTypeDiscovery.IsCandidateType(node),
                transform: static (ctx, ct) => FacadeTypeDiscovery.DiscoverTypeMethods(ctx, ct))
            .Where(static m => m is { IsEmpty: false })
            .SelectMany((m, _) => m!.Value)
            .Collect();

        // Combine method-level and type-level discoveries
        var allCandidateMethods = discoveredMethods
            .Combine(discoveredTypeMethods)
            .Select(static (data, _) => data.Left.AddRange(data.Right));

        // Discover properties that have attributes
        var discoveredProperties = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => FacadePropertyDiscovery.IsCandidateProperty(node),
                transform: static (ctx, ct) => FacadePropertyDiscovery.DiscoverProperties(ctx, ct))
            .Where(static p => p is { IsEmpty: false })
            .SelectMany((p, _) => p!.Value)
            .Collect();

        // Discover properties from types (classes/structs/records) that have attributes
        var discoveredTypeProperties = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => FacadeTypeDiscovery.IsCandidateType(node),
                transform: static (ctx, ct) => FacadeTypeDiscovery.DiscoverTypeProperties(ctx, ct))
            .Where(static p => p is { IsEmpty: false })
            .SelectMany((p, _) => p!.Value)
            .Collect();

        // Combine property-level and type-level property discoveries
        var allCandidateProperties = discoveredProperties
            .Combine(discoveredTypeProperties)
            .Select(static (data, _) => data.Left.AddRange(data.Right));

        // Combine facades with all candidate methods, properties, and compilation
        var combined = discoveredFacades
            .Combine(allCandidateMethods)
            .Combine(allCandidateProperties)
            .Select(static (data, _) => (Facades: data.Left.Left, CandidateMethods: data.Left.Right, CandidateProperties: data.Right))
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(combined, FacadeGenerationPipeline.Execute);
    }
}
