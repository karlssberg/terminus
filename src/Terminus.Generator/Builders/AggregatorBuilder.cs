using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders;

internal static class AggregatorBuilder
{

    internal static NamespaceDeclarationSyntax GenerateAggregatorTypeDeclarations(AggregatorContext aggregatorContext)
    {
        var interfaceNamespace = aggregatorContext.Facade.InterfaceSymbol.ContainingNamespace.ToDisplayString();
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
        return InterfaceDeclaration(aggregatorContext.Facade.InterfaceSymbol.Name)
            .WithModifiers(TokenList(Token(
                    SyntaxKind.PublicKeyword), 
                Token(SyntaxKind.PartialKeyword)))
            .WithMembers(aggregatorContext.FacadeMethodMethodInfos.Select(GenerateFacadeMethodMethodInterfaceDefinition).ToSyntaxList())
            .NormalizeWhitespace();
    }

    private static ClassDeclarationSyntax GenerateAggregatorClassImplementationWithScope(AggregatorContext aggregatorContext)
    {
        var interfaceName = aggregatorContext.Facade.InterfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var implementationClassName = aggregatorContext.Facade.GetImplementationClassName();

        var classDeclaration = ClassDeclaration(implementationClassName)
            .WithModifiers([Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(interfaceName)));

        // Add [FacadeImplementation(typeof(IFacade))] attribute
        var facadeImplAttribute = Attribute(
            ParseName("global::Terminus.FacadeImplementation"),
            AttributeArgumentList(SingletonSeparatedList(
                AttributeArgument(TypeOfExpression(ParseTypeName(interfaceName))))));

        classDeclaration = classDeclaration.WithAttributeLists(
            SingletonList(AttributeList(SingletonSeparatedList(facadeImplAttribute))));

        var members = new List<MemberDeclarationSyntax>
        {
            ParseMemberDeclaration("private readonly global::System.IServiceProvider _serviceProvider;")!
        };

        // Add Dispatcher field and constructor parameter only for scoped facades
        if (aggregatorContext.Facade.Scoped)
        {
            members.Add(ParseMemberDeclaration($"private readonly global::Terminus.Dispatcher<{interfaceName}> _dispatcher;")!);
            members.Add(ParseMemberDeclaration(
                $$"""
                  public {{implementationClassName}}(global::System.IServiceProvider serviceProvider, global::Terminus.Dispatcher<{{interfaceName}}> dispatcher)
                  {
                      _serviceProvider = serviceProvider;
                      _dispatcher = dispatcher;
                  }
                  """)!);
        }
        else
        {
            members.Add(ParseMemberDeclaration(
                $$"""
                  public {{implementationClassName}}(global::System.IServiceProvider serviceProvider)
                  {
                      _serviceProvider = serviceProvider;
                  }
                  """)!);
        }

        members.AddRange(GenerateImplementationFacadeMethods(aggregatorContext));

        return classDeclaration.WithMembers(List(members));
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateImplementationFacadeMethods(AggregatorContext aggregatorContext)
    {
        return aggregatorContext.FacadeMethodMethodInfos.Select(facadeMethod => GenerateFacadeMethodMethodImplementationDefinition(aggregatorContext.Facade, facadeMethod));
    }

    private static MemberDeclarationSyntax GenerateFacadeMethodMethodInterfaceDefinition(CandidateMethodInfo candidate)
    {
        // Emit interface method without access modifiers regardless of source method modifiers
        return candidate.MethodSymbol
            .ToMethodDeclaration()
            .WithModifiers(new SyntaxTokenList())
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private static MemberDeclarationSyntax GenerateFacadeMethodMethodImplementationDefinition(
        FacadeInterfaceInfo facadeInfo,
        CandidateMethodInfo candidate)
    {
        // Build return type
        var returnTypeSyntax = candidate.MethodSymbol.ReturnsVoid
            ? PredefinedType(Token(SyntaxKind.VoidKeyword))
            : ParseTypeName(candidate.MethodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        // Build parameter list
        var parameterList = ParameterList(SeparatedList(
            candidate.MethodSymbol.Parameters.Select(p =>
                Parameter(Identifier(p.Name))
                    .WithType(ParseTypeName(p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))))));

        var method = MethodDeclaration(returnTypeSyntax, Identifier(candidate.MethodSymbol.Name))
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithParameterList(parameterList);

        // Add async modifier when returning Task/Task<T> or when generating an async iterator
        // for IAsyncEnumerable in a scoped facade (we create an async scope and yield items).
        if (candidate.ReturnTypeKind is ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult
            || (candidate.ReturnTypeKind is ReturnTypeKind.AsyncEnumerable && facadeInfo.Scoped))
        {
            method = method.AddModifiers(Token(SyntaxKind.AsyncKeyword));
        }

        method = method.WithBody(Block(
            GenerateFacadeMethodMethodImplementationMethodBody(facadeInfo, candidate)));

        return method.NormalizeWhitespace();
    }

    private static IEnumerable<StatementSyntax> GenerateFacadeMethodMethodImplementationMethodBody(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo candidate)
    {
        var serviceProviderExpression = facadeInfo.Scoped
            ? "scope.ServiceProvider"
            : "_serviceProvider";

        // Build instance/service resolution expression
        var fullyQualifiedTypeName = candidate.MethodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var instanceExpression = ParseExpression(candidate.MethodSymbol.IsStatic
            ? fullyQualifiedTypeName :
            $"{serviceProviderExpression}.GetRequiredService<{fullyQualifiedTypeName}>()");

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
            ReturnTypeKind.AsyncEnumerable when facadeInfo.Scoped =>
                // For scoped async streams, proxy enumeration via the facade interface within the scope
                // as expected by tests: scope.ServiceProvider.GetRequiredService<IFacade>().Method(...)
                ParseStatement(
                    $$"""
                    await foreach (var item in scope.ServiceProvider.GetRequiredService<{{facadeInfo.InterfaceSymbol.Name}}>().{{candidate.MethodSymbol.Name}}({{string.Join(", ", candidate.MethodSymbol.Parameters.Select(p => p.Name))}}))
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

        if (!facadeInfo.Scoped)
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

            { ReturnTypeKind: ReturnTypeKind.AsyncEnumerable } when facadeInfo.DotnetFeatures.HasFlag(DotnetFeature.AsyncDisposable) =>
                GenerateUsingStatementWithCreateAsyncScope(innerStatement),

            { ReturnTypeKind: ReturnTypeKind.AsyncEnumerable } =>
                GenerateUsingStatementWithCreateScope(innerStatement),

            { ReturnTypeKind: ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult } when facadeInfo.DotnetFeatures.HasFlag(DotnetFeature.AsyncDisposable) =>
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