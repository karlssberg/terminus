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
internal sealed class InterfaceBuilder
{
    private readonly FacadeInterfaceInfo _facadeInfo;
    private readonly ImmutableArray<AggregatedMethodGroup> _methodGroups;
    private readonly ImmutableArray<CandidatePropertyInfo> _properties;

    private InterfaceBuilder(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> methodGroups,
        ImmutableArray<CandidatePropertyInfo> properties)
    {
        _facadeInfo = facadeInfo;
        _methodGroups = methodGroups;
        _properties = properties;
    }

    /// <summary>
    /// Builds the partial interface declaration with all method and property signatures.
    /// </summary>
    public static InterfaceDeclarationSyntax Build(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> methodGroups,
        ImmutableArray<CandidatePropertyInfo> properties = default)
    {
        var builder = new InterfaceBuilder(facadeInfo, methodGroups, properties);
        return builder.BuildInternal();
    }

    private InterfaceDeclarationSyntax BuildInternal()
    {
        var interfaceName = _facadeInfo.InterfaceSymbol.Name;

        // Flatten all methods from groups for documentation
        var allMethods = _methodGroups.SelectMany(g => g.Methods).ToImmutableArray();
        var documentation = DocumentationBuilder.BuildInterfaceDocumentation(allMethods, _properties);

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
        var memberDeclarations = BuildInterfaceMembers();

        return interfaceDeclaration.WithMembers(memberDeclarations);
    }

    private SyntaxList<MemberDeclarationSyntax> BuildInterfaceMembers()
    {
        var members = new List<MemberDeclarationSyntax>();

        // Add method declarations
        members.AddRange(_methodGroups.Select(group => MethodSignatureBuilder.BuildInterfaceMethod(_facadeInfo, group)));

        // Add property declarations (sorted by name for consistent output)
        if (!_properties.IsDefault && !_properties.IsEmpty)
        {
            var sortedProperties = _properties.OrderBy(p => p.PropertySymbol.Name).ToList();
            members.AddRange(sortedProperties.Select(prop => PropertySignatureBuilder.BuildInterfaceProperty(_facadeInfo, prop)));
        }

        return List(members);
    }
}
