using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Naming;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

        return MethodDeclaration(returnTypeSyntax, Identifier(methodName))
            .WithParameterList(parameterList)
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

        // Use explicit interface implementation
        var interfaceName = facadeInfo.InterfaceSymbol
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var explicitInterfaceSpecifier = ExplicitInterfaceSpecifier(ParseName(interfaceName));

        var method = MethodDeclaration(returnTypeSyntax, Identifier(methodName))
            .WithExplicitInterfaceSpecifier(explicitInterfaceSpecifier)
            .WithParameterList(parameterList);

        // Add async modifier when returning Task/Task<T> or when generating an async iterator
        // for IAsyncEnumerable in a scoped facade (we create an async scope and yield items).
        if (methodInfo.ReturnTypeKind is ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult
            || (methodInfo.ReturnTypeKind is ReturnTypeKind.AsyncEnumerable && facadeInfo.Scoped))
        {
            method = method.AddModifiers(Token(SyntaxKind.AsyncKeyword));
        }

        return method;
    }

    private static TypeSyntax BuildReturnType(CandidateMethodInfo methodInfo)
    {
        return methodInfo.MethodSymbol.ReturnsVoid
            ? PredefinedType(Token(SyntaxKind.VoidKeyword))
            : ParseTypeName(methodInfo.MethodSymbol.ReturnType
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static ParameterListSyntax BuildParameterList(CandidateMethodInfo methodInfo)
    {
        return ParameterList(SeparatedList(
            methodInfo.MethodSymbol.Parameters.Select(p =>
                Parameter(Identifier(p.Name))
                    .WithType(ParseTypeName(p.Type
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))))));
    }
}
