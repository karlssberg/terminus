using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders;

namespace Terminus.Generator;

[Generator]
public class FacadeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover facade interfaces marked with [FacadeOf]
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
                transform: GetMethodWithFacadeMethodAttribute)
            .Where(static m => m.HasValue)
            .Select((m, _) => m!.Value)
            .Collect();

        // Combine both providers
        var combined = discoveredFacades.Combine(discoveredMethods);

        context.RegisterSourceOutput(combined, Execute);
    }

    private void Execute(
        SourceProductionContext context,
        (ImmutableArray<FacadeInterfaceInfo> Aggregators, ImmutableArray<CandidateMethodInfo> FacadeMethods) data)
    {
        var (aggregators, facadeMethods) = data;

        if (facadeMethods.IsEmpty && aggregators.IsEmpty)
            return;

        // Group entry points by their attribute type (exact match)
        var facadeMethodsByAttributeTypesDictionary = facadeMethods
            .GroupBy(
                ep => ep.AttributeData.AttributeClass!,
                (IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default)
            .ToDictionary(
                g => g.Key,
                g => g.ToImmutableArray(),
                (IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default);

        var validFacades = new List<FacadeInterfaceInfo>();

        foreach (var aggregator in aggregators)
        {
            var facadeMethodMethodInfos = aggregator.FacadeMethodAttributeTypes
                .SelectMany(facadeMethodAttributeType =>
                    facadeMethodsByAttributeTypesDictionary.TryGetValue(facadeMethodAttributeType, out var attrType)
                        ? attrType.AsEnumerable()
                        : [])
                .ToImmutableArray();

            // Filter by TargetTypes if specified
            if (!aggregator.TargetTypes.IsEmpty)
            {
                facadeMethodMethodInfos = 
                [
                    ..facadeMethodMethodInfos
                        .Where(ep => aggregator.TargetTypes.Any(targetType =>
                            SymbolEqualityComparer.Default.Equals(ep.MethodSymbol.ContainingType, targetType)))
                ];
            }

            // Validate entry points for this facade
            var hasErrors = UsageValidator.Validate(context, facadeMethodMethodInfos, aggregator);

            // Skip code generation if there were errors
            if (hasErrors)
                continue;

            // Track valid facades for service registration
            validFacades.Add(aggregator);

            // Generate facade implementation
            var aggregatorContext =
                new AggregatorContext(aggregator)
                {
                    FacadeMethodMethodInfos = facadeMethodMethodInfos,
                };

            var source = SourceBuilder
                .GenerateAggregatorFacadeMethods(aggregatorContext)
                .ToFullString();

            context.AddSource($"{aggregator.InterfaceSymbol.ToIdentifierString()}_Generated.g.cs", source);
        }
    }

    private static bool IsCandidateFacadeInterface(SyntaxNode node) =>
        node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 } @interface &&
        @interface.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

    private static bool IsCandidateMethod(SyntaxNode node) => node is MethodDeclarationSyntax;

    private static FacadeInterfaceInfo? GetFacadeAttributeInfo(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        var asyncDisposableTypeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("System.IAsyncDisposable");
        var dotnetFeatures =
            asyncDisposableTypeSymbol is not null ? DotnetFeature.AsyncDisposable : DotnetFeature.None;

        if (context.SemanticModel.GetDeclaredSymbol(context.Node, ct) is not INamedTypeSymbol interfaceSymbol)
            return null;

        var terminusFacadeOfSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("Terminus.FacadeOfAttribute");

        // Find [FacadeOf] or derived attribute
        foreach (var aggregatorAttrData in interfaceSymbol.GetAttributes())
        {
            if (aggregatorAttrData.AttributeClass == null)
                continue;

            // Check if attribute is or derives from FacadeOfAttribute
            if (!InheritsFromFacadeOfAttribute(aggregatorAttrData.AttributeClass, terminusFacadeOfSymbol))
                continue;

            // Check if this is the official Terminus.FacadeOfAttribute
            var isOfficialAttribute = terminusFacadeOfSymbol != null &&
                SymbolEqualityComparer.Default.Equals(aggregatorAttrData.AttributeClass, terminusFacadeOfSymbol);

            var generationFeatures = new GenerationFeatures(aggregatorAttrData, isOfficialAttribute);

            // Get constructor arguments (first parameter is params Type[] facadeMethodAttributes)
            // For params arrays, the values are in ConstructorArguments[0].Values
            var facadeMethodAttrTypes = aggregatorAttrData.ConstructorArguments.Length > 0
                ? aggregatorAttrData.ConstructorArguments[0].Values
                    .Select(x => x.Value)
                    .OfType<INamedTypeSymbol>()
                    .ToImmutableArray()
                : ImmutableArray<INamedTypeSymbol>.Empty;

            var targetTypes = aggregatorAttrData.ConstructorArguments
                                  .SelectMany(x => x.Values)
                                  .Select(x => x.Value)
                                  .OfType<INamedTypeSymbol>()
                                  .ToImmutableArray();

            return new FacadeInterfaceInfo(
                interfaceSymbol,
                facadeMethodAttrTypes,
                targetTypes,
                dotnetFeatures,
                generationFeatures.IsScoped
            );
        }

        return null;
    }

    private static bool InheritsFromFacadeOfAttribute(INamedTypeSymbol attributeClass, INamedTypeSymbol? facadeOfSymbol)
    {
        var current = attributeClass;
        while (current != null)
        {
            // Check by symbol equality if we have the FacadeOfAttribute symbol
            if (facadeOfSymbol != null && SymbolEqualityComparer.Default.Equals(current, facadeOfSymbol))
                return true;

            // Check by name (handle both with and without "Attribute" suffix)
            var name = current.Name;
            if (name == "FacadeOfAttribute" || name == "FacadeOf")
                return true;

            current = current.BaseType;
        }

        return false;
    }

    private static CandidateMethodInfo? GetMethodWithFacadeMethodAttribute(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax, ct);

        if (methodSymbol == null)
            return null;

        // Check each attribute on the method
        // We collect all methods with any attribute, then filter by facade requirements in Execute
        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass == null)
                continue;

            var returnTypeKind = context.SemanticModel.Compilation.ResolveReturnTypeKind(methodSymbol);
            return new CandidateMethodInfo(
                methodSymbol,
                attributeData,
                returnTypeKind
            );
        }

        return null;
    }

}