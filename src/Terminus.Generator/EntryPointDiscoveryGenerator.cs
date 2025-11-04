using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terminus.Generator;

[Generator]
public class EntryPointDiscoveryGenerator : IIncrementalGenerator
{
    private const string BaseAttributeFullName = "Terminus.Attributes.EntryPointAttribute";
    private const string MediatorAttributeFullName = "Terminus.Attributes.EntryPointMediatorAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover mediator interfaces marked with [EntryPointMediator]
        var discoveredMediators = context.SyntaxProvider
            .ForAttributeWithMetadataName( 
                fullyQualifiedMetadataName: MediatorAttributeFullName,
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

        // Find [EntryPointMediator] or derived attribute
        foreach (var attributeData in interfaceSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass == null)
                continue;

            if (InheritsFromMediatorAttribute(attributeData.AttributeClass))
            {
                // Get the ForEntryPointAttribute type from the mediator attribute
                var entryPointAttrType = GetEntryPointAttributeType(attributeData, context.SemanticModel.Compilation);

                if (entryPointAttrType == null)
                    return null;

                return new MediatorInterfaceInfo(
                    interfaceSymbol,
                    attributeData,
                    entryPointAttrType
                );
            }
        }

        return null;
    }

    private static INamedTypeSymbol? GetEntryPointAttributeType(
        AttributeData mediatorAttribute,
        Compilation compilation)
    {
        // Check constructor argument: [EntryPointMediator(typeof(CommandAttribute))]
        if (mediatorAttribute.ConstructorArguments.Length > 0)
        {
            var arg = mediatorAttribute.ConstructorArguments[0];
            if (arg.Value is INamedTypeSymbol typeSymbol)
                return typeSymbol;
        }

        // Check named property: ForEntryPointAttribute = typeof(CommandAttribute)
        foreach (var namedArg in mediatorAttribute.NamedArguments)
        {
            if (namedArg is { Key: "ForEntryPointAttribute", Value.Value: INamedTypeSymbol typeSymbol })
            {
                return typeSymbol;
            }
        }

        // Default to base EntryPointAttribute
        return compilation.GetTypeByMetadataName(BaseAttributeFullName);
    }

    private static bool InheritsFromMediatorAttribute(INamedTypeSymbol attributeClass)
    {
        var current = attributeClass;

        while (current != null)
        {
            if (current.ToDisplayString() == MediatorAttributeFullName)
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
                return new EntryPointMethodInfo(
                    methodSymbol,
                    attributeData
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
        var entryPointsByAttributeType = entryPoints
            .GroupBy(
                ep => ep.AttributeData.AttributeClass!,
                (IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default)
            .ToDictionary(
                g => g.Key,
                g => g.ToImmutableArray(),
                (IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default);

        // If there are mediators, generate one file per mediator
        if (mediators.IsEmpty)
        {

        }

        foreach (var mediator in mediators)
        {
            // Find all entry points whose attribute type matches or derives from the mediator's target
            var matchingEntryPoints = entryPointsByAttributeType
                .Where(kvp => InheritsFromOrEquals(kvp.Key, mediator.EntryPointAttributeType))
                .SelectMany(kvp => kvp.Value)
                .ToImmutableArray();

            if (matchingEntryPoints.IsEmpty)
            {
                // Report diagnostic: Mediator has no entry points
                var diagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TERM001",
                        "No entry points found",
                        $"Mediator interface '{mediator.InterfaceSymbol.Name}' references " +
                        $"'{mediator.EntryPointAttributeType.Name}' but no methods are marked with this attribute or its derivatives",
                        "Terminus",
                        DiagnosticSeverity.Warning,
                        true),
                    mediator.MediatorAttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation());

                context.ReportDiagnostic(diagnostic);
                continue;
            }

            // Generate mediator implementation
            var source = EntrypointRegistrationSourceBuilder.GenerateForMediator(
                mediator,
                matchingEntryPoints).ToFullString();

            var fileName = $"{mediator.EntryPointAttributeType.Name}_Generated.g.cs";
            context.AddSource(fileName, source);
        }
    }

    private static bool InheritsFromOrEquals(INamedTypeSymbol derived, INamedTypeSymbol baseType)
    {
        var current = derived;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }
        return false;
    }
}