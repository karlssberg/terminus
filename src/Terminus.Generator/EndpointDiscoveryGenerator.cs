using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terminus.Generator;

[Generator]
public class EndpointDiscoveryGenerator : IIncrementalGenerator
{
    private const string BaseAttributeFullName = "Terminus.Attributes.EntryPointAttribute";
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover methods that have an attribute deriving from EndpointAttribute
        var discoveredMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateMethod(node),
                transform: GetMethodWithDerivedAttribute)
            .Where(static m => m.HasValue)
            .Select((m, _) => m!.Value)
            .Collect();

        context.RegisterSourceOutput(discoveredMethods, Execute);
    }

    private static bool IsCandidateMethod(SyntaxNode node) => 
        node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };

    private static EntryPointMethodDescriptor? GetMethodWithDerivedAttribute(
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
                return new EntryPointMethodDescriptor(
                    MethodName: methodSymbol.Name,
                    ContainingType: methodSymbol.ContainingType.ToDisplayString(),
                    AttributeType: attributeData.AttributeClass.ToDisplayString(),
                    AttributeData: ExtractAttributeData(attributeData),
                    IsStatic: methodSymbol.IsStatic
                );
            }
        }

        // Fallback: detect by attribute name syntax to tolerate missing using directives in user code
        foreach (var attrList in methodSyntax.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var nameText = attr.Name.ToString();
                if (nameText == "EntryPoint" || nameText == "EntryPointAttribute")
                {
                    return new EntryPointMethodDescriptor(
                        MethodName: methodSymbol.Name,
                        ContainingType: methodSymbol.ContainingType.ToDisplayString(),
                        AttributeType: "Terminus.Attributes.EntryPointAttribute",
                        AttributeData: new AttributeInfo(default!),
                        IsStatic: methodSymbol.IsStatic
                    );
                }
            }
        }

        return null;
    }


    private static AttributeInfo ExtractAttributeData(AttributeData attributeData)
    {
        return new AttributeInfo(attributeData);
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

    private void Execute(SourceProductionContext context, ImmutableArray<EntryPointMethodDescriptor> entryPoints)
    {
        // Generate one consolidated type for all discovered endpoints
        var source = EntrypointRegistrationSourceBuilder.Generate(entryPoints);
        context.AddSource("EntryPoints.g.cs", source);
    }
}