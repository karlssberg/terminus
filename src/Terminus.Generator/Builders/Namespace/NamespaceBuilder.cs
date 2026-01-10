using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Class;
using Terminus.Generator.Builders.Interface;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Namespace;

/// <summary>
/// Builds the complete namespace declaration containing the interface and implementation class.
/// </summary>
internal static class NamespaceBuilder
{
    /// <summary>
    /// Builds the members (interface and implementation) within their namespace, if applicable.
    /// </summary>
    public static MemberDeclarationSyntax[] Build(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<CandidateMethodInfo> methods)
    {
        var interfaceNamespace = facadeInfo.InterfaceSymbol.ContainingNamespace;

        var interfaceDeclaration = InterfaceBuilder.Build(facadeInfo, methods);
        var classDeclaration = ImplementationClassBuilder.Build(facadeInfo, methods);

        if (interfaceNamespace.IsGlobalNamespace)
        {
            return [interfaceDeclaration, classDeclaration];
        }

        return
        [
            NamespaceDeclaration(ParseName(interfaceNamespace.ToDisplayString()))
                .WithMembers([interfaceDeclaration, classDeclaration])
                .NormalizeWhitespace()
        ];
    }
}
