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
        ImmutableArray<AggregatedMethodGroup> methodGroups,
        ImmutableArray<CandidatePropertyInfo> properties = default)
    {
        var interfaceNamespace = facadeInfo.InterfaceSymbol.ContainingNamespace;

        var interfaceDeclaration = InterfaceBuilder.Build(facadeInfo, methodGroups, properties);
        var classDeclaration = ImplementationClassBuilder.Build(facadeInfo, methodGroups, properties);

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
