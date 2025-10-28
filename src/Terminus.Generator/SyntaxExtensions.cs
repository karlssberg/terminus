using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator;

internal static class SyntaxExtensions
{
    private static readonly SyntaxToken CommaToken = ParseToken(",");
    
    internal static PropertyDeclarationSyntax WithAssignmentTo(
        this PropertyDeclarationSyntax propertyDeclaration,
        ExpressionSyntax expression)
    {
        return propertyDeclaration
            .WithInitializer(EqualsValueClause(expression))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    internal static PropertyDeclarationSyntax WithGetterOnly(
        this PropertyDeclarationSyntax propertyDeclaration)
    {
        return propertyDeclaration
            .AddAccessorListAccessors(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
    }

    internal static PropertyDeclarationSyntax WithModifiers(
        this PropertyDeclarationSyntax propertyDeclaration,
        params SyntaxKind[] modifiers)
    {
        return propertyDeclaration.WithModifiers(TokenList(modifiers.Select(Token)));
    }

    internal static  ObjectCreationExpressionSyntax WithEmptyArguments(
        this ObjectCreationExpressionSyntax expression)
    {
        return expression.WithArgumentList(ArgumentList());
    }
    
    internal static  ObjectCreationExpressionSyntax WithCollectionInitializer(
        this ObjectCreationExpressionSyntax expression,
        IEnumerable<ExpressionSyntax> initializations)
    {
        return expression.WithInitializer(
            InitializerExpression(SyntaxKind.CollectionInitializerExpression)
                .WithExpressions(SeparatedList(initializations, [CommaToken])));
    }
    
    internal static  ObjectCreationExpressionSyntax WithObjectInitializer(
        this ObjectCreationExpressionSyntax expression,
        IEnumerable<ExpressionSyntax> initializations)
    {
        return expression.WithInitializer(
            InitializerExpression(SyntaxKind.ObjectInitializerExpression)
                .WithExpressions(SeparatedList(initializations, [CommaToken])));
    }
    
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

    internal static ArrayCreationExpressionSyntax WithInitializer(
        this ArrayCreationExpressionSyntax expression,
        ExpressionSyntax firstInitialization,
        params IEnumerable<ExpressionSyntax> otherInitializations)
    {
        return expression.WithInitializer([firstInitialization, ..otherInitializations]);
    }
}