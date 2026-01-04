using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders;

internal static class AggregatorBuilder
{

    internal static NamespaceDeclarationSyntax GenerateAggregatorTypeDeclarations(AggregatorContext aggregatorContext)
    {
        var interfaceNamespace = aggregatorContext.Aggregator.InterfaceSymbol.ContainingNamespace.ToDisplayString();
        return NamespaceDeclaration(ParseName(interfaceNamespace))
            .WithMembers(
            [
                GenerateAggregatorInterfaceExtensionDeclaration(aggregatorContext),
                GenerateAggregatorClassImplementationWithScope(aggregatorContext)
            ])
            .NormalizeWhitespace();
    }

    private static InterfaceDeclarationSyntax GenerateAggregatorInterfaceExtensionDeclaration(AggregatorContext aggregatorContext)
    {
        return InterfaceDeclaration(aggregatorContext.Aggregator.InterfaceSymbol.Name)
            .WithModifiers(TokenList(Token(
                    SyntaxKind.PublicKeyword), 
                Token(SyntaxKind.PartialKeyword)))
            .WithMembers(aggregatorContext.EntryPointMethodInfos.Select(GenerateEntryPointMethodInterfaceDefinition).ToSyntaxList())
            .NormalizeWhitespace();
    }

    private static ClassDeclarationSyntax GenerateAggregatorClassImplementationWithScope(AggregatorContext aggregatorContext)
    {
        var interfaceName = aggregatorContext.Aggregator.InterfaceSymbol.ToDisplayString();
        var implementationClassName = aggregatorContext.Aggregator.GetImplementationClassName();
        return ClassDeclaration(implementationClassName)
            .WithModifiers([Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(aggregatorContext.Aggregator.InterfaceSymbol.ToDisplayString())))
            .WithMembers(
            [
                ParseMemberDeclaration("private readonly IServiceProvider _serviceProvider;")!,
                ParseMemberDeclaration(
                    $$"""
                      public {{implementationClassName}}(IServiceProvider serviceProvider)
                      {
                          _serviceProvider = serviceProvider;
                      }
                      """)!,
                ..GenerateImplementationFacadeMethods(aggregatorContext)
            ]);
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateImplementationFacadeMethods(AggregatorContext aggregatorContext)
    {
        return aggregatorContext.EntryPointMethodInfos.Select(entryPoint => GenerateEntryPointMethodImplementationDefinition(aggregatorContext.Aggregator, entryPoint));
    }

    private static MemberDeclarationSyntax GenerateEntryPointMethodInterfaceDefinition(CandidateMethodInfo candidate)
    {
        // Emit interface method without access modifiers regardless of source method modifiers
        return candidate.MethodSymbol
            .ToMethodDeclaration()
            .WithModifiers(new SyntaxTokenList())
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private static MemberDeclarationSyntax GenerateEntryPointMethodImplementationDefinition(
        AggregatorInterfaceInfo aggregatorInfo,
        CandidateMethodInfo candidate)
    {
        // Build return type
        var returnTypeSyntax = candidate.MethodSymbol.ReturnsVoid
            ? PredefinedType(Token(SyntaxKind.VoidKeyword))
            : ParseTypeName(candidate.MethodSymbol.ReturnType.ToDisplayString());

        // Build parameter list
        var parameterList = ParameterList(SeparatedList(
            candidate.MethodSymbol.Parameters.Select(p =>
                Parameter(Identifier(p.Name))
                    .WithType(ParseTypeName(p.Type.ToDisplayString())))));
     
        var method = MethodDeclaration(returnTypeSyntax, Identifier(candidate.MethodSymbol.Name))
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithParameterList(parameterList);

        // Add async modifier when returning Task/Task<T> or when generating an async iterator
        // for IAsyncEnumerable in a scoped facade (we create an async scope and yield items).
        if (candidate.ReturnTypeKind is ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult
            || (candidate.ReturnTypeKind is ReturnTypeKind.AsyncEnumerable && aggregatorInfo.Scoped))
        {
            method = method.AddModifiers(Token(SyntaxKind.AsyncKeyword));
        }

        method = method.WithBody(Block(
            GenerateEntryPointMethodImplementationMethodBody(aggregatorInfo, candidate)));

        return method.NormalizeWhitespace();
    }

    private static IEnumerable<StatementSyntax> GenerateEntryPointMethodImplementationMethodBody(AggregatorInterfaceInfo aggregatorInfo, CandidateMethodInfo candidate)
    {
        var serviceProviderExpression = aggregatorInfo.Scoped
            ? "scope.ServiceProvider"
            : "_serviceProvider";
        
        // Build instance/service resolution expression
        var instanceExpression = ParseExpression(candidate.MethodSymbol.IsStatic
            ? candidate.MethodSymbol.ContainingType.ToDisplayString() : 
            $"{serviceProviderExpression}.GetRequiredService<{candidate.MethodSymbol.ContainingType.ToDisplayString()}>()");

        // Build method invocation
        var methodAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            instanceExpression,
            IdentifierName(candidate.MethodSymbol.Name));
        
        var argumentList = ArgumentList(SeparatedList(
            candidate.MethodSymbol.Parameters.Select(p => Argument(IdentifierName(p.Name)))
        ));

        var invocationExpression = InvocationExpression(methodAccess, argumentList);

        // For async Task/Task<T>, append ConfigureAwait(false) and await the call
        var isAsyncTask = candidate.ReturnTypeKind is ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult;
        if (isAsyncTask)
        {
            invocationExpression = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    invocationExpression,
                    IdentifierName("ConfigureAwait")))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.FalseLiteralExpression)))));
        }

        // Return or expression statement depending on void / async kind
        StatementSyntax innerStatement = candidate.ReturnTypeKind switch
        {
            ReturnTypeKind.Void => ExpressionStatement(invocationExpression),
            ReturnTypeKind.Task => ExpressionStatement(AwaitExpression(invocationExpression)),
            ReturnTypeKind.TaskWithResult => ReturnStatement(AwaitExpression(invocationExpression)),
            ReturnTypeKind.AsyncEnumerable when aggregatorInfo.Scoped =>
                // For scoped async streams, proxy enumeration via the facade interface within the scope
                // as expected by tests: scope.ServiceProvider.GetRequiredService<IFacade>().Method(...)
                ParseStatement(
                    $$"""
                    await foreach (var item in scope.ServiceProvider.GetRequiredService<{{aggregatorInfo.InterfaceSymbol.Name}}>().{{candidate.MethodSymbol.Name}}({{string.Join(", ", candidate.MethodSymbol.Parameters.Select(p => p.Name))}}))
                    {
                        yield return item;
                    }
                    """),
            _ => ReturnStatement(invocationExpression)
        };

        var cancellationTokens = candidate.MethodSymbol.Parameters.Where(p =>
            !p.IsParams && p.Type.ToDisplayString() == typeof(CancellationToken).FullName)
            .ToList();

        if (cancellationTokens.Count == 1)
        {
            var parameterName = cancellationTokens[0].Name;
            yield return ParseStatement($"{parameterName}.ThrowIfCancellationRequested();");
        }

        if (!aggregatorInfo.Scoped)
        {
            yield return innerStatement;
            yield break;
        }

        // In scoped facades, wrap the call into a scope. For async streams, we already built
        // an await-foreach statement as innerStatement to proxy the stream.
        yield return candidate switch
        {
            { MethodSymbol.IsStatic: true } =>
                innerStatement,

            { ReturnTypeKind: ReturnTypeKind.AsyncEnumerable } when aggregatorInfo.DotnetFeatures.HasFlag(DotnetFeature.AsyncDisposable) =>
                GenerateUsingStatementWithCreateAsyncScope(innerStatement),

            { ReturnTypeKind: ReturnTypeKind.AsyncEnumerable } =>
                GenerateUsingStatementWithCreateScope(innerStatement),

            { ReturnTypeKind: ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult } when aggregatorInfo.DotnetFeatures.HasFlag(DotnetFeature.AsyncDisposable) =>
                GenerateUsingStatementWithCreateAsyncScope(innerStatement),

            _ => GenerateUsingStatementWithCreateScope(innerStatement)
        };
    }
    
    private static UsingStatementSyntax GenerateUsingStatementWithCreateScope(StatementSyntax innerStatement)
    {
        // using (var scope = _serviceProvider.CreateScope()) { ... }
        var createScopeAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName("_serviceProvider"),
            IdentifierName("CreateScope"));

        return UsingStatement(Block(innerStatement))
            .WithDeclaration(
                VariableDeclaration(IdentifierName("var"))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(Identifier("scope"))
                            .WithInitializer(EqualsValueClause(
                                InvocationExpression(createScopeAccess))))));
    }

    private static UsingStatementSyntax GenerateUsingStatementWithCreateAsyncScope(StatementSyntax innerStatement)
    {
        // await using (var scope = _serviceProvider.CreateAsyncScope()) { ... }
        var createScopeAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName("_serviceProvider"),
            IdentifierName("CreateAsyncScope"));

        return UsingStatement(Block(innerStatement))
            .WithAwaitKeyword(Token(SyntaxKind.AwaitKeyword))
            .WithDeclaration(
                VariableDeclaration(IdentifierName("var"))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(Identifier("scope"))
                            .WithInitializer(EqualsValueClause(
                                InvocationExpression(createScopeAccess))))));
    }
}