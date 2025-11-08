using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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
        return NamespaceDeclaration(ParseName(interfaceNamespace))
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
        return InterfaceDeclaration(mediatorInfo.InterfaceSymbol.Name)
            .WithModifiers(TokenList(Token(
                    SyntaxKind.PublicKeyword), 
                Token(SyntaxKind.PartialKeyword)))
            .WithMembers(GenerateInterfaceMediatorMethods(entryPoints).ToSyntaxList())
            .NormalizeWhitespace();
    }

    private static ClassDeclarationSyntax GenerateMediatorClassImplementationWithScope(
        MediatorInterfaceInfo mediatorInfo,
        ImmutableArray<EntryPointMethodInfo> entryPoints)
    {
        var entryPointAttributeType = mediatorInfo.EntryPointAttributeType.ToDisplayString();
        var implementationClassName = mediatorInfo.GetImplementationClassName();
        return ClassDeclaration(implementationClassName)
            .WithModifiers([Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(mediatorInfo.InterfaceSymbol.ToDisplayString())))
            .WithMembers(
            [
                ParseMemberDeclaration("private readonly IServiceProvider _serviceProvider;")!,
                ParseMemberDeclaration($"private readonly Terminus.Dispatcher<{entryPointAttributeType}> _dispatcher;")!,
                ParseMemberDeclaration(
                    $$"""
                      public {{implementationClassName}}(IServiceProvider serviceProvider, Terminus.Dispatcher<{{entryPointAttributeType}}> dispatcher)
                      {
                          _serviceProvider = serviceProvider;
                          _dispatcher = dispatcher;
                      }
                      """)!,
                ..GenerateImplementationMediatorMethods(entryPoints)
            ]);
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateInterfaceMediatorMethods(ImmutableArray<EntryPointMethodInfo> entryPoints)
    {
        HashSet<ReturnTypeKind> returnTypeKindsDiscovered = [];
        foreach (var entryPoint in entryPoints)
        {
            returnTypeKindsDiscovered.Add(entryPoint.ReturnTypeKind);
            yield return GenerateEntryPointMethodInterfaceDefinition(entryPoint);
        }
        
        foreach (var returnTypeKind in returnTypeKindsDiscovered)
        {
            yield return returnTypeKind switch
            {
                ReturnTypeKind.Void => GeneratePublishMethodInterfaceDefinition(),
                ReturnTypeKind.Result => GenerateSendMethodInterfaceDefinition(),
                ReturnTypeKind.Task => GeneratePublishAsyncMethodInterfaceDefinition(),
                ReturnTypeKind.TaskWithResult => GenerateSendAsyncMethodInterfaceDefinition(),
                ReturnTypeKind.AsyncEnumerable =>  GenerateStreamAsyncEnumerableMethodInterfaceDefinition(),
                _ => throw new  ArgumentOutOfRangeException(
                    nameof(returnTypeKind),
                    returnTypeKind,
                    $"Return type kind '{Enum.GetName(typeof(ReturnTypeKind), returnTypeKind)}' is unsupported.")
            };
        }
    }
    
    private static IEnumerable<MemberDeclarationSyntax> GenerateImplementationMediatorMethods(ImmutableArray<EntryPointMethodInfo> entryPoints)
    {
        HashSet<ReturnTypeKind> returnTypeKindsDiscovered = [];
        foreach (var entryPoint in entryPoints)
        {
            returnTypeKindsDiscovered.Add(entryPoint.ReturnTypeKind);
            yield return GenerateEntryPointMethodImplementationDefinition(entryPoint);
        }
        
        foreach (var returnTypeKind in returnTypeKindsDiscovered)
        {
            yield return returnTypeKind switch
            {
                ReturnTypeKind.Void => GeneratePublishMethodImplementation(),
                ReturnTypeKind.Result => GenerateSendMethodImplementation(),
                ReturnTypeKind.Task => GeneratePublishAsyncMethodImplementation(),
                ReturnTypeKind.TaskWithResult => GenerateSendAsyncMethodImplementation(),
                ReturnTypeKind.AsyncEnumerable =>  GenerateStreamAsyncEnumerableMethodImplementation(),
                _ => throw new  ArgumentOutOfRangeException(
                    nameof(returnTypeKind),
                    returnTypeKind,
                    $"Return type kind '{Enum.GetName(typeof(ReturnTypeKind), returnTypeKind)}' is unsupported.")
            };
        }
    }

    private static MemberDeclarationSyntax GenerateEntryPointMethodInterfaceDefinition(EntryPointMethodInfo entryPoint)
    {
        return entryPoint.MethodSymbol.ToMethodDeclaration().WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private static MemberDeclarationSyntax GeneratePublishMethodInterfaceDefinition()
    {
        return ParseMemberDeclaration(
            "public void Publish(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default);")!;
    }

    private static MemberDeclarationSyntax GeneratePublishMethodImplementation()
    {
        const string methodDeclaration =
            """
            public void Publish(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _dispatcher.Publish(context, cancellationToken);
            }
            """;

        return ParseMemberDeclaration(methodDeclaration)!;
    }

    private static MemberDeclarationSyntax GeneratePublishAsyncMethodInterfaceDefinition()
    {
        return ParseMemberDeclaration(
            "public System.Threading.Tasks.Task PublishAsync(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default);")!;
    }
    
    private static MemberDeclarationSyntax GeneratePublishAsyncMethodImplementation()
    {
        const string methodDeclaration =
            """
            public System.Threading.Tasks.Task PublishAsync(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return _dispatcher.PublishAsync(context, cancellationToken);
            }
            """;

        return ParseMemberDeclaration(methodDeclaration)!;
    }

    private static MemberDeclarationSyntax GenerateSendMethodInterfaceDefinition()
    {
        return ParseMemberDeclaration(
            "public T Send<T>(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default);")!;
    }

    private static MemberDeclarationSyntax GenerateSendMethodImplementation()
    {
        const string methodDeclaration =
            """
            public T Send<T>(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return _dispatcher.Send<T>(context, cancellationToken);
            }
            """;

        return ParseMemberDeclaration(methodDeclaration)!;
    }

    private static MemberDeclarationSyntax GenerateSendAsyncMethodInterfaceDefinition()
    {
        return ParseMemberDeclaration(
            "public System.Threading.Tasks.Task<T> SendAsync<T>(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default);")!;
    }

    private static MemberDeclarationSyntax GenerateSendAsyncMethodImplementation()
    {
        const string methodDeclaration =
            """
            public System.Threading.Tasks.Task<T> SendAsync<T>(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return _dispatcher.SendAsync<T>(context, cancellationToken);
            }
            """;

        return ParseMemberDeclaration(methodDeclaration)!;
    }

    private static MemberDeclarationSyntax GenerateStreamAsyncEnumerableMethodInterfaceDefinition()
    {
        return ParseMemberDeclaration(
            "public System.Collections.Generic.IAsyncEnumerable<T> CreateStream<T>(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default);")!;
    }

    private static MemberDeclarationSyntax GenerateStreamAsyncEnumerableMethodImplementation()
    {
        const string methodDeclaration =
            """
            public System.Collections.Generic.IAsyncEnumerable<T> CreateStream<T>(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return _dispatcher.CreateStream<T>(context, cancellationToken);
            }
            """;

        return ParseMemberDeclaration(methodDeclaration)!;
    }


    private static ClassDeclarationSyntax GenerateMediatorClassImplementationWithoutContext(
        MediatorInterfaceInfo mediatorInfo,
        ImmutableArray<EntryPointMethodInfo> entryPoints)
    {
        var types = entryPoints
            .Select(ep => ep.MethodSymbol.ContainingType)
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .ToList();
        
        var fields = types
            .Select(type => ParseMemberDeclaration(
                $"private readonly {type.ToDisplayString()} _{type.ToVariableIdentifierString()};")!)
            .ToSyntaxList();

        var constructorParameters = types
            .Select(type => (Identifier(type.ToVariableIdentifierString()), ParseTypeName(type.ToDisplayString())))
            .ToParameterList()
            .NormalizeWhitespace();
        
        var fieldAssignments = types
            .Select(type => type.ToVariableIdentifierString())
            .Select(identifier => ParseStatement(
                $"_{identifier} = {identifier};"))
            .ToSyntaxList();
        
        var implementationClassName = mediatorInfo.GetImplementationClassName();
        return ClassDeclaration(implementationClassName)
            .WithModifiers([Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(mediatorInfo.InterfaceSymbol.ToDisplayString())))
            .WithMembers(
            [
                ..fields,
                ParseMemberDeclaration(
                    $$"""
                      public {{implementationClassName}}{{constructorParameters}}
                      {
                          {{fieldAssignments}}
                      }
                      """)!,
                ..entryPoints.Select(GenerateEntryPointMethodImplementationDefinition),
            ])
            .NormalizeWhitespace();
    }

    private static MemberDeclarationSyntax GenerateEntryPointMethodImplementationDefinition(EntryPointMethodInfo entryPoint)
    {
        // Build return type
        var returnTypeSyntax = entryPoint.MethodSymbol.ReturnsVoid
            ? PredefinedType(Token(SyntaxKind.VoidKeyword))
            : ParseTypeName(entryPoint.MethodSymbol.ReturnType.ToDisplayString());

        // Build parameter list
        var parameterList = ParameterList(SeparatedList(
            entryPoint.MethodSymbol.Parameters.Select(p =>
                Parameter(Identifier(p.Name))
                    .WithType(ParseTypeName(p.Type.ToDisplayString()))
            )));

        // Build instance/service resolution expression
        var instanceExpression = ParseExpression(entryPoint.MethodSymbol.IsStatic
            ? entryPoint.MethodSymbol.ContainingType.ToDisplayString() : 
            $"scope.ServiceProvider.GetRequiredService<{entryPoint.MethodSymbol.ContainingType.ToDisplayString()}>()");

        // Build method invocation
        var methodAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            instanceExpression,
            IdentifierName(entryPoint.MethodSymbol.Name));
     
        var method = MethodDeclaration(returnTypeSyntax, Identifier(entryPoint.MethodSymbol.Name))
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithParameterList(parameterList)
            .WithBody(Block(GenerateBody()));

        return method.NormalizeWhitespace();

        IEnumerable<StatementSyntax> GenerateBody()
        {
            var argumentList = ArgumentList(SeparatedList(
                entryPoint.MethodSymbol.Parameters.Select(p => Argument(IdentifierName(p.Name)))
            ));

            var invocationExpression = InvocationExpression(methodAccess, argumentList);

            // Return or expression statement depending on void
            StatementSyntax innerStatement = entryPoint.MethodSymbol.ReturnsVoid
                ? ExpressionStatement(invocationExpression)
                : ReturnStatement(invocationExpression);

            var usingStatement = entryPoint.MethodSymbol.IsAsync
                ? GenerateUsingStatementWithCreateAsyncScope(innerStatement)
                : GenerateUsingStatementWithCreateScope(innerStatement);
            
            var cancellationTokens = entryPoint.MethodSymbol.Parameters.Where(p =>
                !p.IsParams && p.Type.ToDisplayString() == typeof(CancellationToken).FullName)
                .ToList();

            if (cancellationTokens.Count == 1)
            {
                var parameterName = cancellationTokens[0].Name;
                yield return ParseStatement($"{parameterName}.ThrowIfCancellationRequested();");
            }

            yield return usingStatement;
        }
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