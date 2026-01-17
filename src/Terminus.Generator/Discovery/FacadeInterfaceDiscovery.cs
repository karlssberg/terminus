using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terminus.Generator.Discovery;

/// <summary>
/// Discovers facade interfaces marked with [FacadeOf] or derived attributes.
/// </summary>
internal sealed class FacadeInterfaceDiscovery
{
    /// <summary>
    /// Fast syntax-level check to identify candidate facade interfaces.
    /// </summary>
    public static bool IsCandidateFacadeInterface(SyntaxNode node) =>
        node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 } @interface &&
        @interface.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

    /// <summary>
    /// Performs semantic analysis to discover facade interface with [FacadeOf] attribute.
    /// </summary>
    public static FacadeInterfaceInfo? DiscoverFacadeInterface(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        var asyncDisposableTypeSymbol = context.SemanticModel.Compilation
            .GetTypeByMetadataName("System.IAsyncDisposable");
        var dotnetFeatures = asyncDisposableTypeSymbol is not null 
            ? DotnetFeature.AsyncDisposable 
            : DotnetFeature.None;

        if (context.SemanticModel.GetDeclaredSymbol(context.Node, ct) is not INamedTypeSymbol interfaceSymbol)
            return null;

        var terminusFacadeOfSymbol = context.SemanticModel.Compilation
            .GetTypeByMetadataName("Terminus.FacadeOfAttribute");

        // Find [FacadeOf] or derived attribute
        foreach (var aggregatorAttrData in interfaceSymbol.GetAttributes())
        {
            if (aggregatorAttrData.AttributeClass == null)
                continue;

            // Check if attribute is or derives from FacadeOfAttribute
            if (!InheritsFromFacadeOfAttribute(aggregatorAttrData.AttributeClass, terminusFacadeOfSymbol))
                continue;
            
            var generationFeatures = new GenerationFeatures(aggregatorAttrData);

            // For generic attributes like FacadeOfAttribute<T>, get type arguments
            // For non-generic attributes, get constructor arguments
            ImmutableArray<INamedTypeSymbol> facadeMethodAttrTypes;

            if (aggregatorAttrData.AttributeClass is { IsGenericType: true, TypeArguments.Length: > 0 })
            {
                // Generic attribute: extract type arguments (e.g., FacadeOfAttribute<T> -> T)
                facadeMethodAttrTypes = [
                    ..aggregatorAttrData.AttributeClass.TypeArguments
                        .OfType<INamedTypeSymbol>()
                ];
            }
            else
            {
                // Non-generic attribute: extract constructor arguments
                var allConstructorArguments = aggregatorAttrData.ConstructorArguments;
                facadeMethodAttrTypes = [
                    ..allConstructorArguments
                        .Where(arg => arg.Kind is TypedConstantKind.Type or TypedConstantKind.Array)
                        .SelectMany(arg => arg.Kind == TypedConstantKind.Array ? arg.Values : [arg])
                        .Select(arg => arg.Value)
                        .OfType<INamedTypeSymbol>()
                ];
            }

            // TargetTypes is not supported in the current design - always empty
            var targetTypes = ImmutableArray<INamedTypeSymbol>.Empty;

            return new FacadeInterfaceInfo(
                interfaceSymbol,
                facadeMethodAttrTypes,
                targetTypes,
                dotnetFeatures,
                generationFeatures
            );
        }

        return null;
    }

    private static bool InheritsFromFacadeOfAttribute(
        INamedTypeSymbol attributeClass, 
        INamedTypeSymbol? facadeOfSymbol)
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
}
