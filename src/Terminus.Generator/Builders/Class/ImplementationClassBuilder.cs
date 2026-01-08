using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Method;
using Terminus.Generator.Builders.Strategies;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Class;

/// <summary>
/// Orchestrates the building of the complete facade implementation class.
/// </summary>
internal sealed class ImplementationClassBuilder
{
    /// <summary>
    /// Builds the complete implementation class declaration.
    /// </summary>
    public ClassDeclarationSyntax Build(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<CandidateMethodInfo> methods)
    {
        var interfaceName = facadeInfo.InterfaceSymbol
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var implementationClassName = facadeInfo.GetImplementationClassName();

        var classDeclaration = ClassDeclaration(implementationClassName)
            .WithModifiers([Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(interfaceName)));

        // Determine if we need IServiceProvider based on whether we have instance methods
        var hasInstanceMethods = methods.Any(m => !m.MethodSymbol.IsStatic);

        // Add [FacadeImplementation] attribute
        classDeclaration = AddFacadeImplementationAttribute(
            classDeclaration,
            interfaceName,
            facadeInfo.Scoped,
            hasInstanceMethods);

        // Add disposal interfaces for scoped facades with instance methods
        if (facadeInfo.Scoped && hasInstanceMethods)
        {
            classDeclaration = classDeclaration.AddBaseListTypes(
                SimpleBaseType(ParseTypeName("global::System.IDisposable")),
                SimpleBaseType(ParseTypeName("global::System.IAsyncDisposable")));
        }

        // Build members
        var members = new List<MemberDeclarationSyntax>();

        // Add fields
        switch (facadeInfo.Scoped)
        {
            case true when hasInstanceMethods:
                members.AddRange(FieldBuilder.BuildScopedFields());
                break;
            case false:
                members.AddRange(FieldBuilder.BuildNonScopedFields());
                break;
        }

        // Add constructor
        switch (facadeInfo.Scoped)
        {
            case true when hasInstanceMethods:
                members.Add(ConstructorBuilder.BuildScopedConstructor(implementationClassName));
                break;
            case false:
                members.Add(ConstructorBuilder.BuildNonScopedConstructor(implementationClassName));
                break;
        }

        // Add implementation methods
        members.AddRange(BuildImplementationMethods(facadeInfo, methods));

        // Add disposal methods for scoped facades
        if (facadeInfo.Scoped && hasInstanceMethods)
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
        // For non-scoped facades, always add [FacadeImplementation] attribute
        // For scoped facades, only add if there are instance methods (static-only facades don't need it)
        if (isScoped && !hasInstanceMethods) return classDeclaration;
        
        var facadeImplAttribute = Attribute(
            ParseName("global::Terminus.FacadeImplementation"),
            AttributeArgumentList(SingletonSeparatedList(
                AttributeArgument(TypeOfExpression(ParseTypeName(interfaceName))))));

        classDeclaration = classDeclaration.WithAttributeLists(
            SingletonList(AttributeList(SingletonSeparatedList(facadeImplAttribute))));

        return classDeclaration;
    }

    private static IEnumerable<MemberDeclarationSyntax> BuildImplementationMethods(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<CandidateMethodInfo> methods)
    {
        foreach (var method in methods)
        {
            var strategy = ServiceResolutionStrategyFactory.GetStrategy(facadeInfo, method);
            var methodBuilder = new MethodBuilder(strategy);
            yield return methodBuilder.BuildImplementationMethod(facadeInfo, method);
        }
    }
}
