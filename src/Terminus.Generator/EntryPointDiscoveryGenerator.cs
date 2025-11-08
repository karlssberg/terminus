using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders;

namespace Terminus.Generator;

[Generator]
public class EntryPointDiscoveryGenerator : IIncrementalGenerator
{
    private const string BaseAttributeFullName = "Terminus.EntryPointAttribute";
    private const string AutoGenerateAttributeFullName = "Terminus.EntryPointFacadeAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover mediator interfaces marked with [EntryPointFacade]
        var discoveredMediators = context.SyntaxProvider
            .ForAttributeWithMetadataName( 
                fullyQualifiedMetadataName: AutoGenerateAttributeFullName,
                predicate: static (node, _) => IsCandidateMediatorInterface(node),
                transform: GetMediatorInterfaceInfo)
            .Where(static m => m.HasValue)
            .Select((m, _) => m!.Value)
            .Collect();

        // Discover methods that have an attribute deriving from EntryPointAttribute
        var discoveredMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateMethod(node),
                transform: GetMethodWithDerivedAttribute)
            .Where(static m => m.HasValue)
            .Select((m, _) => m!.Value)
            .Collect();

        // Combine both providers
        var combined = discoveredMediators.Combine(discoveredMethods);

        context.RegisterSourceOutput(combined, Execute);
    }

    private static bool IsCandidateMethod(SyntaxNode node) =>
        node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };

    private static bool IsCandidateMediatorInterface(SyntaxNode node) =>
        node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 } @interface &&
        @interface.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

    private static MediatorInterfaceInfo? GetMediatorInterfaceInfo(
        GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol interfaceSymbol)
            return null;

        // Find [EntryPointFacade] or derived attribute
        foreach (var attributeData in interfaceSymbol.GetAttributes())
        {
            if (!InheritsFromAutoGenerateAttribute(attributeData.AttributeClass)) continue;
            
            // Get the EntryPointAttribute type from the mediator attribute
            var entryPointAttrType = GetEntryPointAttributeType(attributeData, context.SemanticModel.Compilation);

            if (entryPointAttrType == null)
                return null;

            return new MediatorInterfaceInfo(
                interfaceSymbol,
                attributeData,
                entryPointAttrType
            );
        }

        return null;
    }

    private static INamedTypeSymbol? GetEntryPointAttributeType(
        AttributeData EntryPointFacadeAttribute,
        Compilation compilation)
    {
        // Check EntryPointFacadeAttribute's named property: EntryPointAttribute = typeof(CommandAttribute)
        foreach (var namedArg in EntryPointFacadeAttribute.NamedArguments)
        {
            if (namedArg is { Key: "EntryPointAttribute", Value.Value: INamedTypeSymbol typeSymbol })
            {
                return typeSymbol;
            }
        }
        
        // Default to base EntryPointAttribute
        return compilation.GetTypeByMetadataName(BaseAttributeFullName);
    }

    private static bool InheritsFromAutoGenerateAttribute(INamedTypeSymbol? attributeClass)
    {
        var current = attributeClass;

        while (current is not null)
        {
            if (current.ToDisplayString() == AutoGenerateAttributeFullName)
                return true;

            current = current.BaseType;
        }

        return false;
    }

    private static EntryPointMethodInfo? GetMethodWithDerivedAttribute(
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

            // Walk up the inheritance chain to check if it derives from our base
            if (InheritsFromBaseAttribute(attributeData.AttributeClass))
            {
                var isTaskLike = context.SemanticModel.Compilation.ResolveReturnTypeKind(methodSymbol);
                return new EntryPointMethodInfo(
                    methodSymbol,
                    attributeData,
                    isTaskLike
                );
            }
        }

        return null;
    }
    
    private static bool InheritsFromBaseAttribute(INamedTypeSymbol attributeClass)
    {
        var current = attributeClass;
        
        while (current != null)
        {
            if (current.ToDisplayString() == BaseAttributeFullName)
                return true;

            current = current.BaseType;
        }

        return false;
    }

    private static void Execute(
        SourceProductionContext context,
        (ImmutableArray<MediatorInterfaceInfo> Mediators, ImmutableArray<EntryPointMethodInfo> EntryPoints) data)
    {
        var (mediators, entryPoints) = data;

        if (entryPoints.IsEmpty && mediators.IsEmpty)
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
        
        var autoGenerateAttributeTypesDictionary = mediators
            .GroupBy(m => m.EntryPointAttributeType, (IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default)
            .ToDictionary(
                g => g.Key,
                g => g.ToImmutableArray(),
                (IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default);
        

        foreach (var keyValuePair in entryPointsByAttributeTypesDictionary)
        {
            var entryPointAttributeType = keyValuePair.Key!;
            var entryPointMethodInfos = keyValuePair.Value;
            
            // Generate mediator implementation
            var source = SourceBuilder
                .GenerateEntryPoints(
                    entryPointAttributeType,
                    entryPointMethodInfos,
                    autoGenerateAttributeTypesDictionary.TryGetValue(entryPointAttributeType, out var entryPointAttributeMediators)
                        ? entryPointAttributeMediators
                        : ImmutableArray<MediatorInterfaceInfo>.Empty)
                .ToFullString();

            context.AddSource($"{entryPointAttributeType.ToIdentifierString()}_Generated.g.cs", source);
        }

        var compilationUnitSyntax = SourceBuilder
            .GenerateServiceRegistrations([..entryPointsByAttributeTypesDictionary.Keys])
            .NormalizeWhitespace();
        
        context.AddSource("__EntryPointServiceRegistration_Generated.g.cs", compilationUnitSyntax.ToFullString());
    }
}