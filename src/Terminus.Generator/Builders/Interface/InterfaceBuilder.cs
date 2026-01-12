using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Attributes;
using Terminus.Generator.Builders.Documentation;
using Terminus.Generator.Builders.Method;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Interface;

/// <summary>
/// Builds the partial interface declaration with facade method signatures.
/// </summary>
internal static class InterfaceBuilder
{
    /// <summary>
    /// Builds the partial interface declaration with all method signatures.
    /// </summary>
    public static InterfaceDeclarationSyntax Build(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> methodGroups)
    {
        var interfaceName = facadeInfo.InterfaceSymbol.Name;

        // Flatten all methods from groups for documentation
        var allMethods = methodGroups.SelectMany(g => g.Methods).ToImmutableArray();
        var documentation = DocumentationBuilder.BuildInterfaceDocumentation(allMethods);

        var attributeList = GeneratedCodeAttributeBuilder.Build();

        // Add documentation as leading trivia to the attribute list
        if (documentation.Any())
        {
            attributeList = attributeList.WithLeadingTrivia((IEnumerable<SyntaxTrivia>)documentation);
        }

        var interfaceDeclaration = InterfaceDeclaration(interfaceName)
            .WithAttributeLists(SingletonList(attributeList))
            .WithModifiers(TokenList(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.PartialKeyword)));

        // Build interface method declarations
        var memberDeclarations = BuildInterfaceMembers(facadeInfo, methodGroups);

        return interfaceDeclaration.WithMembers(memberDeclarations);
    }

    private static SyntaxList<MemberDeclarationSyntax> BuildInterfaceMembers(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> methodGroups)
    {
        return methodGroups
            .Select(group => MethodSignatureBuilder.BuildInterfaceMethod(facadeInfo, group))
            .ToSyntaxList<MemberDeclarationSyntax>();
    }
}
