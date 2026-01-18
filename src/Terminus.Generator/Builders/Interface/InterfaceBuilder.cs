using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Attributes;
using Terminus.Generator.Builders.Documentation;
using Terminus.Generator.Builders.Method;
using Terminus.Generator.Builders.Property;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Interface;

/// <summary>
/// Builds the partial interface declaration with facade method and property signatures.
/// </summary>
internal static class InterfaceBuilder
{
    /// <summary>
    /// Builds the partial interface declaration with all method and property signatures.
    /// </summary>
    public static InterfaceDeclarationSyntax Build(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> methodGroups,
        ImmutableArray<CandidatePropertyInfo> properties = default)
    {
        var interfaceName = facadeInfo.InterfaceSymbol.Name;

        // Flatten all methods from groups for documentation
        var allMethods = methodGroups.SelectMany(g => g.Methods).ToImmutableArray();
        var documentation = DocumentationBuilder.BuildInterfaceDocumentation(allMethods, properties);

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

        // Build interface member declarations (methods and properties)
        var memberDeclarations = BuildInterfaceMembers(facadeInfo, methodGroups, properties);

        return interfaceDeclaration.WithMembers(memberDeclarations);
    }

    private static SyntaxList<MemberDeclarationSyntax> BuildInterfaceMembers(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> methodGroups,
        ImmutableArray<CandidatePropertyInfo> properties)
    {
        var members = new List<MemberDeclarationSyntax>();

        // Add method declarations
        members.AddRange(methodGroups.Select(group => MethodSignatureBuilder.BuildInterfaceMethod(facadeInfo, group)));

        // Add property declarations (sorted by name for consistent output)
        if (!properties.IsDefault && !properties.IsEmpty)
        {
            var sortedProperties = properties.OrderBy(p => p.PropertySymbol.Name).ToList();
            members.AddRange(sortedProperties.Select(prop => PropertySignatureBuilder.BuildInterfaceProperty(facadeInfo, prop)));
        }

        return List(members);
    }
}
