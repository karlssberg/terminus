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

        // Discover methods that have attributes - we'll filter later
        var discoveredMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateMethod(node),
                transform: GetCandidateMethodsWithAttributes)
            .Where(static m => m.HasValue && !m.Value.IsEmpty)
            .SelectMany((m, _) => m!.Value)
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

        foreach (var aggregator in aggregators)
        {
            // Match methods where the attribute is or inherits from the specified FacadeMethodAttributeTypes
            var facadeMethodMethodInfos = facadeMethods
                .Where(method => aggregator.FacadeMethodAttributeTypes.Any(facadeAttrType =>
                    InheritsFromAttribute(method.AttributeData.AttributeClass!, facadeAttrType)))
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

            // Generate facade implementation
            var aggregatorContext =
                new AggregatorContext(aggregator)
                {
                    FacadeMethodMethodInfos = facadeMethodMethodInfos,
                };

            var orchestrator = new FacadeBuilderOrchestrator();
            var source = orchestrator
                .Generate(aggregatorContext)
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

            // ConstructorArguments: [0] = first type, [1...] = params Type[] or following types
            var allConstructorArguments = aggregatorAttrData.ConstructorArguments;
            var facadeMethodAttrTypes = allConstructorArguments
                .Where(arg => arg.Kind == TypedConstantKind.Type || arg.Kind == TypedConstantKind.Array)
                .SelectMany(arg => arg.Kind == TypedConstantKind.Array ? arg.Values : [arg])
                .Select(arg => arg.Value)
                .OfType<INamedTypeSymbol>()
                .ToImmutableArray();

            // TargetTypes is not supported in the current design - always empty
            var targetTypes = ImmutableArray<INamedTypeSymbol>.Empty;

            return new FacadeInterfaceInfo(
                interfaceSymbol,
                facadeMethodAttrTypes,
                targetTypes,
                dotnetFeatures,
                generationFeatures.IsScoped,
                generationFeatures.CommandName,
                generationFeatures.QueryName,
                generationFeatures.AsyncCommandName,
                generationFeatures.AsyncQueryName,
                generationFeatures.AsyncStreamName
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

    private static bool InheritsFromAttribute(INamedTypeSymbol attributeClass, INamedTypeSymbol targetAttributeType)
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

    private static ImmutableArray<CandidateMethodInfo>? GetCandidateMethodsWithAttributes(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax, ct);

        if (methodSymbol == null)
            return null;

        var attributes = methodSymbol.GetAttributes();
        if (attributes.IsEmpty)
            return null;

        var returnTypeKind = context.SemanticModel.Compilation.ResolveReturnTypeKind(methodSymbol);
        
        // Return one CandidateMethodInfo per attribute on the method
        return attributes
            .Where(attr => attr.AttributeClass != null)
            .Select(attr => new CandidateMethodInfo(methodSymbol, attr, returnTypeKind))
            .ToImmutableArray();
    }

}