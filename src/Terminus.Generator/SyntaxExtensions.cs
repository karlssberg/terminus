using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator;

internal static class SyntaxExtensions
{
    internal static  InvocationExpressionSyntax WithArguments(
        this InvocationExpressionSyntax invocation,
        IEnumerable<ExpressionSyntax> expressions)
    {
        var arguments = expressions.Select(Argument);
        var argumentsList = arguments as IReadOnlyList<ArgumentSyntax> ?? arguments.ToList();

        return argumentsList switch
        {
            { Count: 1 } =>
                invocation.WithArgumentList(ArgumentList(SingletonSeparatedList(argumentsList[0]))),
            _ => invocation.WithArgumentList(ArgumentList(SeparatedList(argumentsList)))
        };
    }

    internal static InvocationExpressionSyntax WithArguments(
        this InvocationExpressionSyntax invocation,
        ExpressionSyntax firstExpression,
        params IEnumerable<ExpressionSyntax> otherExpressions)
    {
        return invocation.WithArguments([firstExpression, .. otherExpressions]);
    }

    internal static ArrayTypeSyntax WithoutRank(this ArrayTypeSyntax expression)
    {
        return expression.WithRankSpecifiers(
            SingletonList(
                ArrayRankSpecifier(
                    SingletonSeparatedList<ExpressionSyntax>(
                        OmittedArraySizeExpression()))));
    }

    internal static ArrayCreationExpressionSyntax WithInitializer(
        this ArrayCreationExpressionSyntax expression,
        IEnumerable<ExpressionSyntax> initializations)
    {
        return expression.WithInitializer(
            InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SeparatedList(initializations)));
    }

    internal static MethodDeclarationSyntax ToMethodDeclarationSyntax(this IMethodSymbol methodSymbol)
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
                    methodSymbol.Parameters.ToParameterListSyntax());
    }

    internal static ParameterListSyntax ToParameterListSyntax(this IEnumerable<IParameterSymbol> parameterSymbols)
    {
        var parameters = parameterSymbols.Select(parameter =>
            Parameter(Identifier(parameter.Name))
                .WithType(ParseTypeName(
                    parameter.Type.ToDisplayString())));
        
        return ParameterList(SeparatedList(parameters));
    }

    internal static SyntaxList<T> ToListSyntax<T>(this IEnumerable<T> items) where T : SyntaxNode
    {
        return [..items];
    }
}