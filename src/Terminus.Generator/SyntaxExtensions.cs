using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator;

internal static class SyntaxExtensions
{
    internal static MethodDeclarationSyntax ToMethodDeclaration(this IMethodSymbol methodSymbol)
    {
        var methodDeclarationSyntax = 
            MethodDeclaration(
                    ParseTypeName(methodSymbol.ReturnType.ToDisplayString()),
                    methodSymbol.Name)
                .WithReturnType(ParseTypeName(
                    methodSymbol.ReturnType.ToDisplayString()));
        
        if (methodSymbol.Parameters.Length == 0)
            return methodDeclarationSyntax;
        
        return
            methodDeclarationSyntax
                .WithParameterList(
                    methodSymbol.Parameters.ToParameterList());
    }

    internal static ParameterListSyntax ToParameterList(this IEnumerable<IParameterSymbol> parameterSymbols)
    {
        return parameterSymbols
            .Select(parameter => (Identifier(parameter.Name), ParseTypeName(parameter.Type.ToDisplayString())))
            .ToParameterList();
    }
    
    internal static ParameterListSyntax ToParameterList(this IEnumerable<(SyntaxToken Identifier, TypeSyntax Type)> parameterMetadata)
    {
        var parameters = parameterMetadata.Select(parameter =>
            Parameter(parameter.Identifier)
                .WithType(parameter.Type));
        
        return ParameterList(SeparatedList(parameters));
    }

    internal static SyntaxList<T> ToSyntaxList<T>(this IEnumerable<T> items) where T : SyntaxNode
    {
        return [..items];
    }
    
    internal static ArgumentListSyntax ToArgumentList<T>(this IEnumerable<T> items) where T : ExpressionSyntax
    {
        return ArgumentList([..items.Select(Argument)]);
    }
    
}