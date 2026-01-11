using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Documentation;
using Terminus.Generator.Builders.Naming;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Terminus.Generator.Builders.Method;

/// <summary>
/// Builds method signature (return type, name, parameters) for facade methods.
/// </summary>
internal sealed class MethodSignatureBuilder
{
    /// <summary>
    /// Builds a method declaration with signature only (no body).
    /// </summary>
    public static MethodDeclarationSyntax BuildInterfaceMethod(
        FacadeInterfaceInfo facadeInfo,
        CandidateMethodInfo methodInfo)
    {
        var returnTypeSyntax = BuildReturnType(methodInfo);
        var methodName = MethodNamingStrategy.GetMethodName(facadeInfo, methodInfo);
        var parameterList = BuildParameterList(methodInfo);
        var typeParameterList = BuildTypeParameterList(methodInfo);
        var typeParameterConstraintList = BuildTypeParameterConstraintList(methodInfo);
        var documentation = DocumentationBuilder.BuildMethodDocumentation(facadeInfo, methodInfo);

        return MethodDeclaration(returnTypeSyntax, Identifier(methodName))
            .WithLeadingTrivia(documentation)
            .WithTypeParameterList(typeParameterList)
            .WithParameterList(parameterList)
            .WithConstraintClauses(typeParameterConstraintList)
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    /// <summary>
    /// Builds a method declaration stub for implementation (signature + explicit interface + modifiers).
    /// </summary>
    public static MethodDeclarationSyntax BuildImplementationMethodStub(
        FacadeInterfaceInfo facadeInfo,
        CandidateMethodInfo methodInfo)
    {
        var returnTypeSyntax = BuildReturnType(methodInfo);
        var methodName = MethodNamingStrategy.GetMethodName(facadeInfo, methodInfo);
        var parameterList = BuildParameterList(methodInfo);
        var typeParameterList = BuildTypeParameterList(methodInfo);

        // Use explicit interface implementation
        var interfaceName = facadeInfo.InterfaceSymbol
            .ToDisplayString(FullyQualifiedFormat);
        var explicitInterfaceSpecifier = ExplicitInterfaceSpecifier(ParseName(interfaceName));

        var method = MethodDeclaration(returnTypeSyntax, Identifier(methodName))
            .WithExplicitInterfaceSpecifier(explicitInterfaceSpecifier)
            .WithTypeParameterList(typeParameterList)
            .WithParameterList(parameterList);

        // Maybe use async keyword
        if (ShouldUseAsyncKeyword(facadeInfo, methodInfo))
        {
            method = method.AddModifiers(Token(SyntaxKind.AsyncKeyword));
        }

        return method;
    }

    private static bool ShouldUseAsyncKeyword(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo)
    {
        // Add async modifier when returning Task/ValueTask/Task<T>/ValueTask<T> or when generating an async iterator
        // for IAsyncEnumerable in a scoped facade (we create an async scope and yield items).
        if (methodInfo.ReturnTypeKind is ReturnTypeKind.AsyncEnumerable && facadeInfo.Scoped)
            return true;

        return methodInfo.ReturnTypeKind is ReturnTypeKind.Task
                                         or ReturnTypeKind.TaskWithResult
                                         or ReturnTypeKind.ValueTask
                                         or ReturnTypeKind.ValueTaskWithResult;
    }

    private static TypeSyntax BuildReturnType(CandidateMethodInfo methodInfo)
    {
        return methodInfo.MethodSymbol.ReturnsVoid
            ? PredefinedType(Token(SyntaxKind.VoidKeyword))
            : ParseTypeName(methodInfo.MethodSymbol.ReturnType
                .ToDisplayString(FullyQualifiedFormat));
    }

    private static ParameterListSyntax BuildParameterList(CandidateMethodInfo methodInfo)
    {
        return ParameterList(SeparatedList(
            methodInfo.MethodSymbol.Parameters.Select(p =>
            {
                var parameter = Parameter(Identifier(p.Name.EscapeIdentifier()))
                    .WithType(ParseTypeName(p.Type
                        .ToDisplayString(FullyQualifiedFormat)));

                if (p.RefKind == RefKind.In)
                {
                    parameter = parameter.AddModifiers(Token(SyntaxKind.InKeyword));
                }

                if (p.HasExplicitDefaultValue)
                {
                    var defaultValue = p.ExplicitDefaultValue switch
                    {
                        string s => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(s)),
                        bool b => LiteralExpression(b ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression),
                        null => LiteralExpression(SyntaxKind.NullLiteralExpression),
                        _ => ParseExpression(p.ExplicitDefaultValue.ToString())
                    };

                    parameter = parameter.WithDefault(EqualsValueClause(defaultValue));
                }

                return parameter;
            })));
    }

    private static TypeParameterListSyntax? BuildTypeParameterList(CandidateMethodInfo methodInfo)
    {
        if (!methodInfo.MethodSymbol.IsGenericMethod)
            return null;

        return TypeParameterList(SeparatedList(
            methodInfo.MethodSymbol.TypeParameters.Select(tp =>
                TypeParameter(Identifier(tp.Name.EscapeIdentifier())))));
    }

    private static SyntaxList<TypeParameterConstraintClauseSyntax> BuildTypeParameterConstraintList(CandidateMethodInfo methodInfo)
    {
        if (!methodInfo.MethodSymbol.IsGenericMethod)
            return default;

        var clauses = methodInfo.MethodSymbol.TypeParameters
            .Select(BuildTypeParameterConstraintClause)
            .Where(c => c != null)
            .Cast<TypeParameterConstraintClauseSyntax>();

        return List(clauses);
    }

    private static TypeParameterConstraintClauseSyntax? BuildTypeParameterConstraintClause(ITypeParameterSymbol tp)
    {
        var constraints = new List<TypeParameterConstraintSyntax>();

        if (tp.HasReferenceTypeConstraint)
            constraints.Add(ClassOrStructConstraint(SyntaxKind.ClassConstraint));
        else if (tp.HasValueTypeConstraint)
            constraints.Add(ClassOrStructConstraint(SyntaxKind.StructConstraint));

        constraints.AddRange(tp.ConstraintTypes
            .Select(typeConstraint => 
                TypeConstraint(ParseTypeName(typeConstraint.ToDisplayString(FullyQualifiedFormat)))).Cast<TypeParameterConstraintSyntax>());

        if (tp.HasConstructorConstraint)
            constraints.Add(ConstructorConstraint());

        if (constraints.Count == 0)
            return null;

        return TypeParameterConstraintClause(IdentifierName(tp.Name.EscapeIdentifier()))
            .WithConstraints(SeparatedList(constraints));
    }
}
