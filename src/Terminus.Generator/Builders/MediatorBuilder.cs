using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders;

internal static class FacadeBuilder
{
    internal static SyntaxList<NamespaceDeclarationSyntax> GenerateFacadeTypeDeclarations(ImmutableArray<EntryPointMethodInfo> entryPointMethodInfos, ImmutableArray<FacadeInterfaceInfo> facades)
    {
        return facades
            .Select(facade => GenerateFacadeTypeDeclarations(facade, entryPointMethodInfos))
            .ToSyntaxList();
    }
    
    private static NamespaceDeclarationSyntax GenerateFacadeTypeDeclarations(FacadeInterfaceInfo facade, ImmutableArray<EntryPointMethodInfo> matchingEntryPoints)
    {
        var interfaceNamespace = facade.InterfaceSymbol.ContainingNamespace.ToDisplayString();
        return NamespaceDeclaration(ParseName(interfaceNamespace))
            .WithMembers(
            [
                GenerateFacadeInterfaceExtensionDeclaration(facade, matchingEntryPoints),
                GenerateFacadeClassImplementationWithScope(facade, matchingEntryPoints)
            ])
            .NormalizeWhitespace();
    }

    private static InterfaceDeclarationSyntax GenerateFacadeInterfaceExtensionDeclaration(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<EntryPointMethodInfo> entryPoints)
    {
        return InterfaceDeclaration(facadeInfo.InterfaceSymbol.Name)
            .WithModifiers(TokenList(Token(
                    SyntaxKind.PublicKeyword), 
                Token(SyntaxKind.PartialKeyword)))
            .WithMembers(GenerateInterfaceFacadeMethods(entryPoints).ToSyntaxList())
            .NormalizeWhitespace();
    }

    private static ClassDeclarationSyntax GenerateFacadeClassImplementationWithScope(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<EntryPointMethodInfo> entryPoints)
    {
        var entryPointAttributeType = facadeInfo.EntryPointAttributeType.ToDisplayString();
        var implementationClassName = facadeInfo.GetImplementationClassName();
        return ClassDeclaration(implementationClassName)
            .WithModifiers([Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(facadeInfo.InterfaceSymbol.ToDisplayString())))
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
                ..GenerateImplementationFacadeMethods(facadeInfo, entryPoints)
            ]);
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateInterfaceFacadeMethods(
        ImmutableArray<EntryPointMethodInfo> entryPoints)
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
    
    private static IEnumerable<MemberDeclarationSyntax> GenerateImplementationFacadeMethods(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<EntryPointMethodInfo> entryPoints)
    {
        HashSet<ReturnTypeKind> returnTypeKindsDiscovered = [];
        foreach (var entryPoint in entryPoints)
        {
            returnTypeKindsDiscovered.Add(entryPoint.ReturnTypeKind);
            yield return GenerateEntryPointMethodImplementationDefinition(facadeInfo, entryPoint);
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
        // Emit interface method without access modifiers regardless of source method modifiers
        return entryPoint.MethodSymbol
            .ToMethodDeclaration()
            .WithModifiers(new SyntaxTokenList())
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private static MemberDeclarationSyntax GeneratePublishMethodInterfaceDefinition()
    {
        return ParseMemberDeclaration(
            "void Publish(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default);")!;
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
            "System.Threading.Tasks.Task PublishAsync(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default);")!;
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
            "T Send<T>(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default);")!;
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
            "System.Threading.Tasks.Task<T> SendAsync<T>(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default);")!;
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
            "System.Collections.Generic.IAsyncEnumerable<T> CreateStream<T>(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default);")!;
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


    private static ClassDeclarationSyntax GenerateFacadeClassImplementationWithoutContext(
        FacadeInterfaceInfo facadeInfo,
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
        
        var implementationClassName = facadeInfo.GetImplementationClassName();
        return ClassDeclaration(implementationClassName)
            .WithModifiers([Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(facadeInfo.InterfaceSymbol.ToDisplayString())))
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
                ..entryPoints.Select(ep => GenerateEntryPointMethodImplementationDefinition(facadeInfo, ep)),
            ])
            .NormalizeWhitespace();
    }

    private static MemberDeclarationSyntax GenerateEntryPointMethodImplementationDefinition(
        FacadeInterfaceInfo facadeInfo,
        EntryPointMethodInfo entryPoint)
    {
        // Build return type
        var returnTypeSyntax = entryPoint.MethodSymbol.ReturnsVoid
            ? PredefinedType(Token(SyntaxKind.VoidKeyword))
            : ParseTypeName(entryPoint.MethodSymbol.ReturnType.ToDisplayString());

        // Build parameter list
        var parameterList = ParameterList(SeparatedList(
            entryPoint.MethodSymbol.Parameters.Select(p =>
                Parameter(Identifier(p.Name))
                    .WithType(ParseTypeName(p.Type.ToDisplayString())))));
     
        var method = MethodDeclaration(returnTypeSyntax, Identifier(entryPoint.MethodSymbol.Name))
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithParameterList(parameterList);

        // Add async modifier when returning Task or Task<T>
        if (entryPoint.ReturnTypeKind is ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult)
        {
            method = method.AddModifiers(Token(SyntaxKind.AsyncKeyword));
        }

        method = method.WithBody(Block(GenerateBody()));

        return method.NormalizeWhitespace();

        IEnumerable<StatementSyntax> GenerateBody()
        {
            var serviceProviderExpression = facadeInfo.Scoped
                ? "scope.ServiceProvider"
                : "_serviceProvider";
            
            // Build instance/service resolution expression
            var instanceExpression = ParseExpression(entryPoint.MethodSymbol.IsStatic
                ? entryPoint.MethodSymbol.ContainingType.ToDisplayString() : 
                $"{serviceProviderExpression}.GetRequiredService<{entryPoint.MethodSymbol.ContainingType.ToDisplayString()}>()");

            // Build method invocation
            var methodAccess = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                instanceExpression,
                IdentifierName(entryPoint.MethodSymbol.Name));
            
            var argumentList = ArgumentList(SeparatedList(
                entryPoint.MethodSymbol.Parameters.Select(p => Argument(IdentifierName(p.Name)))
            ));

            var invocationExpression = InvocationExpression(methodAccess, argumentList);

            // For async Task/Task<T>, append ConfigureAwait(false) and await the call
            var isAsyncTask = entryPoint.ReturnTypeKind is ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult;
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
            StatementSyntax innerStatement = entryPoint.ReturnTypeKind switch
            {
                ReturnTypeKind.Void => ExpressionStatement(invocationExpression),
                ReturnTypeKind.Task => ExpressionStatement(AwaitExpression(invocationExpression)),
                ReturnTypeKind.TaskWithResult => ReturnStatement(AwaitExpression(invocationExpression)),
                _ => ReturnStatement(invocationExpression)
            };

            var cancellationTokens = entryPoint.MethodSymbol.Parameters.Where(p =>
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

            yield return entryPoint switch
            {
                { MethodSymbol.IsStatic: true } =>
                    innerStatement,
                
                { ReturnTypeKind: ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult or ReturnTypeKind.AsyncEnumerable } 
                    when facadeInfo.DotnetFeatures.HasFlag(DotnetFeature.AsyncDisposable) => 
                    GenerateUsingStatementWithCreateAsyncScope(innerStatement),
                
                _ => GenerateUsingStatementWithCreateScope(innerStatement)
            };
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