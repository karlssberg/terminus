using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Attributes;
using Terminus.Generator.Builders.Documentation;
using Terminus.Generator.Builders.Method;
using Terminus.Generator.Builders.Strategies;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Class;

/// <summary>
/// Orchestrates the building of the complete facade implementation class.
/// </summary>
internal static class ImplementationClassBuilder
{
    /// <summary>
    /// Builds the complete implementation class declaration.
    /// </summary>
    public static ClassDeclarationSyntax Build(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> methodGroups)
    {
        var interfaceName = facadeInfo.InterfaceSymbol
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var implementationClassName = facadeInfo.GetImplementationClassName();

        // Flatten all methods from groups for documentation and analysis
        var allMethods = methodGroups.SelectMany(g => g.Methods).ToImmutableArray();
        var documentation = DocumentationBuilder.BuildImplementationDocumentation(allMethods);

        var classDeclaration = ClassDeclaration(implementationClassName)
            .WithModifiers([Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(interfaceName)));

        if (documentation.Any())
        {
            classDeclaration = classDeclaration.WithLeadingTrivia((IEnumerable<SyntaxTrivia>)documentation);
        }

        // Determine if we need IServiceProvider based on whether we have instance methods
        var hasInstanceMethods = allMethods.Any(m => !m.MethodSymbol.IsStatic);

        // Add [FacadeImplementation] attribute
        classDeclaration = AddFacadeImplementationAttribute(
            classDeclaration,
            interfaceName,
            facadeInfo.Features.IsScoped,
            hasInstanceMethods);

        // Add disposal interfaces for scoped facades with instance methods
        if (facadeInfo.Features.IsScoped && hasInstanceMethods)
        {
            classDeclaration = classDeclaration.AddBaseListTypes(
                SimpleBaseType(ParseTypeName("global::System.IDisposable")),
                SimpleBaseType(ParseTypeName("global::System.IAsyncDisposable")));
        }

        // Build members
        var members = new List<MemberDeclarationSyntax>();

        // Add fields
        if (facadeInfo.Features.IsScoped && hasInstanceMethods)
            members.AddRange(FieldBuilder.BuildScopedFields());
        else if (!facadeInfo.Features.IsScoped)
            members.AddRange(FieldBuilder.BuildNonScopedFields());

        // Add constructor
        if (facadeInfo.Features.IsScoped && hasInstanceMethods)
            members.Add(ConstructorBuilder.BuildScopedConstructor(implementationClassName));
        else if (!facadeInfo.Features.IsScoped)
            members.Add(ConstructorBuilder.BuildNonScopedConstructor(implementationClassName));

        // Add implementation methods
        members.AddRange(BuildImplementationMethods(facadeInfo, methodGroups));

        // Add disposal methods for scoped facades
        if (facadeInfo.Features.IsScoped && hasInstanceMethods)
        {
            members.AddRange(DisposalBuilder.BuildDisposalMethods());
        }

        return classDeclaration.WithMembers(List(members));
    }

    private static ClassDeclarationSyntax AddFacadeImplementationAttribute(
        ClassDeclarationSyntax classDeclaration,
        string interfaceName,
        bool isScoped,
        bool hasInstanceMethods)
    {
        var attributeLists = new List<AttributeListSyntax>();

        // Always add [GeneratedCode] attribute
        attributeLists.Add(GeneratedCodeAttributeBuilder.Build());

        // For non-scoped facades, always add [FacadeImplementation] attribute
        // For scoped facades, only add if there are instance methods (static-only facades don't need it)
        if (!isScoped || hasInstanceMethods)
        {
            var facadeImplAttribute = Attribute(
                ParseName("global::Terminus.FacadeImplementation"),
                AttributeArgumentList(SingletonSeparatedList(
                    AttributeArgument(TypeOfExpression(ParseTypeName(interfaceName))))));

            attributeLists.Add(AttributeList(SingletonSeparatedList(facadeImplAttribute)));
        }

        classDeclaration = classDeclaration.WithAttributeLists(List(attributeLists));

        return classDeclaration;
    }

    private static IEnumerable<MemberDeclarationSyntax> BuildImplementationMethods(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> methodGroups)
    {
        return methodGroups.Select(group =>
        {
            // Use the primary method for strategy determination
            var strategy = ServiceResolutionStrategyFactory.GetStrategy(facadeInfo, group.PrimaryMethod);
            var methodBuilder = new MethodBuilder(strategy);
            return methodBuilder.BuildImplementationMethod(facadeInfo, group);
        });
    }
}
