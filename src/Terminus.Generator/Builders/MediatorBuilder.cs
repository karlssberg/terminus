using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terminus.Generator.Builders;

internal static class MediatorBuilder
{
    internal static SyntaxList<NamespaceDeclarationSyntax> GenerateMediatorTypeDeclarations(ImmutableArray<EntryPointMethodInfo> entryPointMethodInfos, ImmutableArray<MediatorInterfaceInfo> mediators)
    {
        return mediators
            .Select(mediator => GenerateMediatorTypeDeclarations(mediator, entryPointMethodInfos))
            .ToSyntaxList();
    }
    
    private static NamespaceDeclarationSyntax GenerateMediatorTypeDeclarations(MediatorInterfaceInfo mediator, ImmutableArray<EntryPointMethodInfo> matchingEntryPoints)
    {
        var interfaceNamespace = mediator.InterfaceSymbol.ContainingNamespace.ToDisplayString();
        return SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(interfaceNamespace))
            .WithMembers(
            [
                GenerateMediatorInterfaceExtensionDeclaration(mediator, matchingEntryPoints),
                GenerateMediatorClassImplementationWithScope(mediator, matchingEntryPoints)
            ])
            .NormalizeWhitespace();
    }

    private static InterfaceDeclarationSyntax GenerateMediatorInterfaceExtensionDeclaration(
        MediatorInterfaceInfo mediatorInfo,
        ImmutableArray<EntryPointMethodInfo> entryPoints)
    {
        return SyntaxFactory.InterfaceDeclaration(mediatorInfo.InterfaceSymbol.Name)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(
                    SyntaxKind.PublicKeyword), 
                SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
            .WithMembers(
                entryPoints
                    .Select(ep => ep.MethodSymbol.ToMethodDeclaration().WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                    .ToSyntaxList<MemberDeclarationSyntax>())
            .NormalizeWhitespace();
    }

    private static ClassDeclarationSyntax GenerateMediatorClassImplementationWithScope(
        MediatorInterfaceInfo mediatorInfo,
        ImmutableArray<EntryPointMethodInfo> entryPoints)
    {
        var implementationClassName = mediatorInfo.GetImplementationClassName();
        return SyntaxFactory.ClassDeclaration(implementationClassName)
            .WithModifiers([SyntaxFactory.Token(SyntaxKind.InternalKeyword), SyntaxFactory.Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(mediatorInfo.InterfaceSymbol.ToDisplayString())))
            .WithMembers(
            [
                SyntaxFactory.ParseMemberDeclaration("private readonly IServiceProvider _serviceProvider;")!,
                SyntaxFactory.ParseMemberDeclaration(
                    $$"""
                      public {{implementationClassName}}(IServiceProvider serviceProvider)
                      {
                          _serviceProvider = serviceProvider;
                      }
                      """)!,
                ..ImmutableArrayExtensions.Select<EntryPointMethodInfo, MemberDeclarationSyntax>(entryPoints, GenerateMediatorMethodImplementation),
            ]);
    }

    private static ClassDeclarationSyntax GenerateMediatorClassImplementation(
        MediatorInterfaceInfo mediatorInfo,
        ImmutableArray<EntryPointMethodInfo> entryPoints)
    {
        var types = entryPoints
            .Select(ep => ep.MethodSymbol.ContainingType)
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .ToList();
        
        var fields = types
            .Select(type => SyntaxFactory.ParseMemberDeclaration(
                $"private readonly {type.ToDisplayString()} _{type.ToVariableIdentifierString()};")!)
            .ToSyntaxList();

        var constructorParameters = types
            .Select(type => (SyntaxFactory.Identifier(type.ToVariableIdentifierString()), SyntaxFactory.ParseTypeName(type.ToDisplayString())))
            .ToParameterList()
            .NormalizeWhitespace();
        
        var fieldAssignments = types
            .Select(type => type.ToVariableIdentifierString())
            .Select(identifier => SyntaxFactory.ParseStatement(
                $"_{identifier} = {identifier};"))
            .ToSyntaxList();
        
        var implementationClassName = mediatorInfo.GetImplementationClassName();
        return SyntaxFactory.ClassDeclaration(implementationClassName)
            .WithModifiers([SyntaxFactory.Token(SyntaxKind.InternalKeyword), SyntaxFactory.Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(mediatorInfo.InterfaceSymbol.ToDisplayString())))
            .WithMembers(
            [
                ..fields,
                SyntaxFactory.ParseMemberDeclaration(
                    $$"""
                      public {{implementationClassName}}{{constructorParameters}}
                      {
                          {{fieldAssignments}}
                      }
                      """)!,
                ..ImmutableArrayExtensions.Select<EntryPointMethodInfo, MemberDeclarationSyntax>(entryPoints, GenerateMediatorMethodImplementation),
            ])
            .NormalizeWhitespace();
    }

    private static MemberDeclarationSyntax GenerateMediatorMethodImplementation(EntryPointMethodInfo entryPoint)
    {
        // Build return type
        var returnTypeSyntax = entryPoint.MethodSymbol.ReturnsVoid
            ? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))
            : SyntaxFactory.ParseTypeName(entryPoint.MethodSymbol.ReturnType.ToDisplayString());

        // Build parameter list
        var parameterList = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
            entryPoint.MethodSymbol.Parameters.Select(p =>
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(p.Name))
                    .WithType(SyntaxFactory.ParseTypeName(p.Type.ToDisplayString()))
            )));

        // Build instance/service resolution expression
        var instanceExpression = SyntaxFactory.ParseExpression(entryPoint.MethodSymbol.IsStatic
            ? entryPoint.MethodSymbol.ContainingType.ToDisplayString() : 
            $"scope.ServiceProvider.GetRequiredService<{entryPoint.MethodSymbol.ContainingType.ToDisplayString()}>()");

        // Build method invocation
        var methodAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            instanceExpression,
            SyntaxFactory.IdentifierName(entryPoint.MethodSymbol.Name));

        var argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            entryPoint.MethodSymbol.Parameters.Select(p => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Name)))
        ));

        var invocationExpression = SyntaxFactory.InvocationExpression(methodAccess, argumentList);

        // Return or expression statement depending on void
        StatementSyntax innerStatement = entryPoint.MethodSymbol.ReturnsVoid
            ? SyntaxFactory.ExpressionStatement(invocationExpression)
            : SyntaxFactory.ReturnStatement(invocationExpression);

        var usingStatement = entryPoint.MethodSymbol.IsAsync
            ? GenerateUsingStatementWithCreateAsyncScope(innerStatement)
            : GenerateUsingStatementWithCreateScope(innerStatement);

        var body = SyntaxFactory.Block(usingStatement);

        var method = SyntaxFactory.MethodDeclaration(returnTypeSyntax, SyntaxFactory.Identifier(entryPoint.MethodSymbol.Name))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(parameterList)
            .WithBody(body);

        return method.NormalizeWhitespace();
    }

    private static UsingStatementSyntax GenerateUsingStatementWithCreateScope(StatementSyntax innerStatement)
    {
        // using (var scope = _serviceProvider.CreateScope()) { ... }
        var createScopeAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("_serviceProvider"),
            SyntaxFactory.IdentifierName("CreateScope"));

        return SyntaxFactory.UsingStatement(SyntaxFactory.Block(innerStatement))
            .WithDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("scope"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(createScopeAccess))))));
    }

    private static UsingStatementSyntax GenerateUsingStatementWithCreateAsyncScope(StatementSyntax innerStatement)
    {
        // await using (var scope = _serviceProvider.CreateAsyncScope()) { ... }
        var createScopeAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("_serviceProvider"),
            SyntaxFactory.IdentifierName("CreateAsyncScope"));

        return SyntaxFactory.UsingStatement(SyntaxFactory.Block(innerStatement))
            .WithAwaitKeyword(SyntaxFactory.Token(SyntaxKind.AwaitKeyword))
            .WithDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("scope"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.InvocationExpression(createScopeAccess))))));
    }
}