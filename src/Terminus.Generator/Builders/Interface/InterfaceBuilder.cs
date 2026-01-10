using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Attributes;
using Terminus.Generator.Builders.Documentation;
using Terminus.Generator.Builders.Method;
using Terminus.Generator.Builders.Strategies;
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
        ImmutableArray<CandidateMethodInfo> methods)
    {
        var interfaceName = facadeInfo.InterfaceSymbol.Name;
        var documentation = DocumentationBuilder.BuildInterfaceDocumentation(methods);

        var attributeList = GeneratedCodeAttributeBuilder.Build();
        
        // Add documentation as leading trivia to the attribute list
        if (documentation.Any())
        {
            attributeList = attributeList.WithLeadingTrivia(documentation);
        }

        var interfaceDeclaration = InterfaceDeclaration(interfaceName)
            .WithAttributeLists(SingletonList(attributeList))
            .WithModifiers(TokenList(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.PartialKeyword)));

        // Build interface method declarations
        var memberDeclarations = BuildInterfaceMembers(facadeInfo, methods);

        return interfaceDeclaration.WithMembers(memberDeclarations);
    }

    private static SyntaxList<MemberDeclarationSyntax> BuildInterfaceMembers(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<CandidateMethodInfo> methods)
    {
        // For interface methods, we don't need service resolution, so we can use a dummy strategy
        var signatureBuilder = new MethodSignatureBuilder();

        return methods
            .Select(method => MethodSignatureBuilder.BuildInterfaceMethod(facadeInfo, method))
            .ToSyntaxList<MemberDeclarationSyntax>();
    }
}
