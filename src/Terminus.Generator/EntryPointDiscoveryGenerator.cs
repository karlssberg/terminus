using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders;

namespace Terminus.Generator;

[Generator]
public class EntryPointDiscoveryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover facade interfaces marked with [EntryPointFacade]
        var discoveredFacades = context.SyntaxProvider
            .CreateSyntaxProvider( 
                predicate: static (node, _) => IsCandidateFacadeInterface(node),
                transform: GetFacadeAttributeInfo)
            .Where(static m => m.HasValue)
            .Select((m, _) => m!.Value)
            .Collect();

        // Discover methods that have an attribute deriving from entry point attribute
        var discoveredMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateMethod(node),
                transform: GetMethodWithEntryPointAttribute)
            .Where(static m => m.HasValue)
            .Select((m, _) => m!.Value)
            .Collect();

        // Combine both providers
        var combined = discoveredFacades.Combine(discoveredMethods);

        context.RegisterSourceOutput(combined, Execute);
    }

    private void Execute(
        SourceProductionContext context,
        (ImmutableArray<AggregatorInterfaceInfo> Aggregators, ImmutableArray<CandidateMethodInfo> EntryPoints) data)
    {
        var (aggregators, entryPoints) = data;

        if (entryPoints.IsEmpty && aggregators.IsEmpty)
            return;

        // Group entry points by their attribute type (exact match)
        var entryPointsByAttributeTypesDictionary = entryPoints
            .GroupBy(
                ep => ep.AttributeData.AttributeClass!,
                (IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default)
            .ToDictionary(
                g => g.Key,
                g => g.ToImmutableArray(),
                (IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default);

        var validFacades = new List<AggregatorInterfaceInfo>();

        foreach (var aggregator in aggregators)
        {
            var entryPointMethodInfos = aggregator.EntryPointAttributeTypes
                .SelectMany(entryPointAttributeType =>
                    entryPointsByAttributeTypesDictionary.TryGetValue(entryPointAttributeType, out var attrType)
                        ? attrType.AsEnumerable()
                        : [])
                .ToImmutableArray();

            // Filter by TargetTypes if specified
            if (!aggregator.TargetTypes.IsEmpty)
            {
                entryPointMethodInfos = 
                [
                    ..entryPointMethodInfos
                        .Where(ep => aggregator.TargetTypes.Any(targetType =>
                            SymbolEqualityComparer.Default.Equals(ep.MethodSymbol.ContainingType, targetType)))
                ];
            }

            // Validate entry points for this facade
            var hasErrors = UsageValidator.Validate(context, entryPointMethodInfos, aggregator);

            // Skip code generation if there were errors
            if (hasErrors)
                continue;

            // Track valid facades for service registration
            validFacades.Add(aggregator);

            // Generate facade implementation
            var aggregatorContext =
                new AggregatorContext(aggregator)
                {
                    EntryPointMethodInfos = entryPointMethodInfos,
                };

            var source = SourceBuilder
                .GenerateAggregatorEntryPoints(aggregatorContext)
                .ToFullString();

            context.AddSource($"{aggregator.InterfaceSymbol.ToIdentifierString()}_Generated.g.cs", source);
        }
    }

    private static bool IsCandidateFacadeInterface(SyntaxNode node) =>
        node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 } @interface &&
        @interface.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

    private static bool IsCandidateMethod(SyntaxNode node) => node is MethodDeclarationSyntax;

    private static AggregatorInterfaceInfo? GetFacadeAttributeInfo(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        var asyncDisposableTypeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("System.IAsyncDisposable");
        var dotnetFeatures = 
            asyncDisposableTypeSymbol is not null ? DotnetFeature.AsyncDisposable : DotnetFeature.None;

        if (context.SemanticModel.GetDeclaredSymbol(context.Node, ct) is not INamedTypeSymbol interfaceSymbol)
            return null;

        // Find [EntryPointFacade] or derived attribute
        foreach (var aggregatorAttrData in interfaceSymbol.GetAttributes())
        {

            var generationFeatures = new GenerationFeatures(aggregatorAttrData);

            var entryPointAttrTypes = aggregatorAttrData.ConstructorArguments
                .Select(x => x.Value)
                .OfType<INamedTypeSymbol>();

            var targetTypes = aggregatorAttrData.ConstructorArguments
                                  .SelectMany(x => x.Values)
                                  .Select(x => x.Value)
                                  .OfType<INamedTypeSymbol>();

            return new AggregatorInterfaceInfo(
                interfaceSymbol,
                [..entryPointAttrTypes],
                [..targetTypes],
                dotnetFeatures,
                generationFeatures.IsScoped
            );
        }

        return null;
    }

    private static CandidateMethodInfo? GetMethodWithEntryPointAttribute(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax, ct);

        if (methodSymbol == null)
            return null;

        // Check each attribute on the method
        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass == null)
                continue;

            var isTaskLike = context.SemanticModel.Compilation.ResolveReturnTypeKind(methodSymbol);
            return new CandidateMethodInfo(
                methodSymbol,
                attributeData,
                isTaskLike
            );
        }

        return null;
    }

}