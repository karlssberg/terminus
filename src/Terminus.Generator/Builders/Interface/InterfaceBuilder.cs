using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Method;
using Terminus.Generator.Builders.Strategies;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Interface;

/// <summary>
/// Builds the partial interface declaration with facade method signatures.
/// </summary>
internal sealed class InterfaceBuilder
{
    /// <summary>
    /// Builds the partial interface declaration with all method signatures.
    /// </summary>
    public InterfaceDeclarationSyntax Build(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<CandidateMethodInfo> methods)
    {
        var interfaceName = facadeInfo.InterfaceSymbol.Name;

        var interfaceDeclaration = InterfaceDeclaration(interfaceName)
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
            .Select(method => signatureBuilder.BuildInterfaceMethod(facadeInfo, method))
            .ToSyntaxList<MemberDeclarationSyntax>();
    }
}
