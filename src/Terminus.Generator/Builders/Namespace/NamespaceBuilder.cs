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
internal sealed class NamespaceBuilder
{
    private readonly InterfaceBuilder _interfaceBuilder;
    private readonly ImplementationClassBuilder _classBuilder;

    public NamespaceBuilder()
    {
        _interfaceBuilder = new InterfaceBuilder();
        _classBuilder = new ImplementationClassBuilder();
    }

    /// <summary>
    /// Builds the complete namespace with interface and implementation.
    /// </summary>
    public NamespaceDeclarationSyntax Build(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<CandidateMethodInfo> methods)
    {
        var interfaceNamespace = facadeInfo.InterfaceSymbol.ContainingNamespace.ToDisplayString();

        var interfaceDeclaration = _interfaceBuilder.Build(facadeInfo, methods);
        var classDeclaration = _classBuilder.Build(facadeInfo, methods);

        return NamespaceDeclaration(ParseName(interfaceNamespace))
            .WithMembers([interfaceDeclaration, classDeclaration])
            .NormalizeWhitespace();
    }
}
