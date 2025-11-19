using System.Collections.Immutable;
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
            .WithMembers(GenerateInterfaceFacadeMethods(aggregatorContext).ToSyntaxList())
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
                ParseMemberDeclaration($"private readonly Terminus.Dispatcher<{interfaceName}> _dispatcher;")!,
                ParseMemberDeclaration(
                    $$"""
                      public {{implementationClassName}}(IServiceProvider serviceProvider, Terminus.Dispatcher<{{interfaceName}}> dispatcher)
                      {
                          _serviceProvider = serviceProvider;
                          _dispatcher = dispatcher;
                      }
                      """)!,
                ..GenerateImplementationFacadeMethods(aggregatorContext)
            ]);
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateInterfaceFacadeMethods(
        AggregatorContext aggregatorContext)
    {
        HashSet<ReturnTypeKind> returnTypeKindsDiscovered = 
            [..aggregatorContext.EntryPointMethodInfos.Select(ep => ep.ReturnTypeKind)];
        
        switch (aggregatorContext.Aggregator.ServiceKind)
        {
            case ServiceKind.Facade:
            {
                foreach (var entryPoint in aggregatorContext.EntryPointMethodInfos)
                {
                    returnTypeKindsDiscovered.Add(entryPoint.ReturnTypeKind);
                    yield return GenerateEntryPointMethodInterfaceDefinition(entryPoint);
                }

                break;
            }
            case ServiceKind.Mediator:
            {
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

                break;
            }
            case ServiceKind.Router:
            {
                yield return GenerateRouteMethodInterfaceDefinition();
                break;
            }
            case ServiceKind.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private static IEnumerable<MemberDeclarationSyntax> GenerateImplementationFacadeMethods(AggregatorContext aggregatorContext)
    {
        HashSet<ReturnTypeKind> returnTypeKindsDiscovered = 
            [..aggregatorContext.EntryPointMethodInfos.Select(ep => ep.ReturnTypeKind)];

        switch (aggregatorContext.Aggregator.ServiceKind)
        {
            case ServiceKind.Facade:
            {
                foreach (var entryPoint in aggregatorContext.EntryPointMethodInfos)
                {
                    yield return GenerateEntryPointMethodImplementationDefinition(aggregatorContext.Aggregator, entryPoint);
                }

                break;
            }
            case ServiceKind.Mediator:
            {
                foreach (var returnTypeKind in returnTypeKindsDiscovered)
                {
                    yield return returnTypeKind switch
                    {
                        ReturnTypeKind.Void => GeneratePublishMethodImplementation(aggregatorContext),
                        ReturnTypeKind.Result => GenerateSendMethodImplementation(aggregatorContext),
                        ReturnTypeKind.Task => GeneratePublishAsyncMethodImplementation(aggregatorContext),
                        ReturnTypeKind.TaskWithResult => GenerateSendAsyncMethodImplementation(aggregatorContext),
                        ReturnTypeKind.AsyncEnumerable => GenerateStreamAsyncEnumerableMethodImplementation(aggregatorContext),
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(returnTypeKind),
                            returnTypeKind,
                            $"Return type kind '{Enum.GetName(typeof(ReturnTypeKind), returnTypeKind)}' is unsupported.")
                    };
                }

                break;
            }
            case ServiceKind.Router:
            {
                yield return GenerateRouteMethodImplementation(aggregatorContext);
                break;
            }
            case ServiceKind.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
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
            "void Publish(System.Collections.Generic.IReadOnlyDictionary<string, object?> arguments, System.Threading.CancellationToken cancellationToken = default);")!;
    }

    private static MemberDeclarationSyntax GeneratePublishMethodImplementation(AggregatorContext aggregatorContext)
    {
        var statement = ParseStatement("_dispatcher.Publish(arguments, cancellationToken);");
        if (aggregatorContext.Aggregator.Scoped)
        {
            statement = GenerateUsingStatementWithCreateScope(statement).NormalizeWhitespace();
        }
            
        var methodDeclaration =
            $$"""
              public void Publish(System.Collections.Generic.IReadOnlyDictionary<string, object?> arguments, System.Threading.CancellationToken cancellationToken = default)
              {
                  cancellationToken.ThrowIfCancellationRequested();
                  {{statement}}
              }
              """;

        return ParseMemberDeclaration(methodDeclaration)!;
    }

    private static MemberDeclarationSyntax GeneratePublishAsyncMethodInterfaceDefinition()
    {
        return ParseMemberDeclaration(
            "System.Threading.Tasks.Task PublishAsync(System.Collections.Generic.IReadOnlyDictionary<string, object?> arguments, System.Threading.CancellationToken cancellationToken = default);")!;
    }
    
    private static MemberDeclarationSyntax GeneratePublishAsyncMethodImplementation(AggregatorContext aggregatorContext)
    {
        var statement = ParseStatement("return _dispatcher.PublishAsync(arguments, cancellationToken);");
        if (aggregatorContext.Aggregator.Scoped)
        {
            statement = aggregatorContext.Aggregator.DotnetFeatures.HasFlag(DotnetFeature.AsyncDisposable) 
                ? GenerateUsingStatementWithCreateAsyncScope(statement).NormalizeWhitespace()
                : GenerateUsingStatementWithCreateScope(statement).NormalizeWhitespace();;
        }
        
        var  methodDeclaration =
            $$"""
              public System.Threading.Tasks.Task PublishAsync(System.Collections.Generic.IReadOnlyDictionary<string, object?> arguments, System.Threading.CancellationToken cancellationToken = default)
              {
                  cancellationToken.ThrowIfCancellationRequested();
                  {{statement}}
              }
              """;

        return ParseMemberDeclaration(methodDeclaration)!;
    }

    private static MemberDeclarationSyntax GenerateSendMethodInterfaceDefinition()
    {
        return ParseMemberDeclaration(
            "T Send<T>(System.Collections.Generic.IReadOnlyDictionary<string, object?> arguments, System.Threading.CancellationToken cancellationToken = default);")!;
    }

    private static MemberDeclarationSyntax GenerateSendMethodImplementation(AggregatorContext aggregatorContext)
    {
        var statement = ParseStatement("return _dispatcher.Send<T>(arguments, cancellationToken);");
        if (aggregatorContext.Aggregator.Scoped)
        {
            statement = GenerateUsingStatementWithCreateScope(statement).NormalizeWhitespace();
        }
        var methodDeclaration =
            $$"""
              public T Send<T>(System.Collections.Generic.IReadOnlyDictionary<string, object?> arguments, System.Threading.CancellationToken cancellationToken = default)
              {
                  cancellationToken.ThrowIfCancellationRequested();
                  {{statement}}
              }
              """;

        return ParseMemberDeclaration(methodDeclaration)!;
    }

    private static MemberDeclarationSyntax GenerateSendAsyncMethodInterfaceDefinition()
    {
        return ParseMemberDeclaration(
            "System.Threading.Tasks.Task<T> SendAsync<T>(System.Collections.Generic.IReadOnlyDictionary<string, object?> arguments, System.Threading.CancellationToken cancellationToken = default);")!;
    }

    private static MemberDeclarationSyntax GenerateSendAsyncMethodImplementation(AggregatorContext aggregatorContext)
    {
        var statement = ParseStatement("return _dispatcher.SendAsync<T>(arguments, cancellationToken);");
        if (aggregatorContext.Aggregator.Scoped)
        {
            statement = aggregatorContext.Aggregator.DotnetFeatures.HasFlag(DotnetFeature.AsyncDisposable) 
                ? GenerateUsingStatementWithCreateAsyncScope(statement).NormalizeWhitespace()
                : GenerateUsingStatementWithCreateScope(statement).NormalizeWhitespace();;
        }
        
        var methodDeclaration =
            $$"""
              public System.Threading.Tasks.Task<T> SendAsync<T>(System.Collections.Generic.IReadOnlyDictionary<string, object?> arguments, System.Threading.CancellationToken cancellationToken = default)
              {
                  cancellationToken.ThrowIfCancellationRequested();
                  {{statement}}
              }
              """;

        return ParseMemberDeclaration(methodDeclaration)!;
    }

    private static MemberDeclarationSyntax GenerateStreamAsyncEnumerableMethodInterfaceDefinition()
    {
        return ParseMemberDeclaration(
            "System.Collections.Generic.IAsyncEnumerable<T> CreateStream<T>(System.Collections.Generic.IReadOnlyDictionary<string, object?> arguments, System.Threading.CancellationToken cancellationToken = default);")!;
    }

    private static MemberDeclarationSyntax GenerateStreamAsyncEnumerableMethodImplementation(
        AggregatorContext aggregatorContext)
    {
        var statement = ParseStatement("return _dispatcher.CreateStream<T>(arguments, cancellationToken);");
        if (aggregatorContext.Aggregator.Scoped)
        {
            statement = aggregatorContext.Aggregator.DotnetFeatures.HasFlag(DotnetFeature.AsyncDisposable) 
                ? GenerateUsingStatementWithCreateAsyncScope(statement).NormalizeWhitespace()
                : GenerateUsingStatementWithCreateScope(statement).NormalizeWhitespace();;
        }
        var methodDeclaration =
            $$"""
              public System.Collections.Generic.IAsyncEnumerable<T> CreateStream<T>(System.Collections.Generic.IReadOnlyDictionary<string, object?> arguments, System.Threading.CancellationToken cancellationToken = default)
              {
                  cancellationToken.ThrowIfCancellationRequested();
                  {{statement}}
              }
              """;

        return ParseMemberDeclaration(methodDeclaration)!;
    }

    private static MemberDeclarationSyntax GenerateRouteMethodInterfaceDefinition()
    {
        return ParseMemberDeclaration(
            "System.Threading.Tasks.Task<RouteResult> Route(System.Collections.Generic.IReadOnlyDictionary<string, object?> arguments, System.Threading.CancellationToken cancellationToken = default);")!;
    }

    private static MemberDeclarationSyntax GenerateRouteMethodImplementation(AggregatorContext aggregatorContext)
    {
        var statement = ParseStatement("return _dispatcher.Route(arguments, cancellationToken);");
        if (aggregatorContext.Aggregator.Scoped)
        {
            statement = GenerateUsingStatementWithCreateScope(statement).NormalizeWhitespace();
        }
        
        var methodDeclaration =
            $$"""
              public System.Threading.Tasks.Task<RouteResult> Route(System.Collections.Generic.IReadOnlyDictionary<string, object?> arguments, System.Threading.CancellationToken cancellationToken = default)
              {
                  cancellationToken.ThrowIfCancellationRequested();
                  {{statement}}
              }
              """;

        return ParseMemberDeclaration(methodDeclaration)!;
    }


    private static ClassDeclarationSyntax GenerateAggregatorClassImplementationWithoutContext(
        AggregatorInterfaceInfo aggregatorInfo,
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
        
        var implementationClassName = aggregatorInfo.GetImplementationClassName();
        return ClassDeclaration(implementationClassName)
            .WithModifiers([Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(aggregatorInfo.InterfaceSymbol.ToDisplayString())))
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
                ..entryPoints.Select(ep => GenerateEntryPointMethodImplementationDefinition(aggregatorInfo, ep)),
            ])
            .NormalizeWhitespace();
    }

    private static MemberDeclarationSyntax GenerateEntryPointMethodImplementationDefinition(
        AggregatorInterfaceInfo aggregatorInfo,
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

        // Add async modifier when returning Task/Task<T> or when generating an async iterator
        // for IAsyncEnumerable in a scoped facade (we create an async scope and yield items).
        if (entryPoint.ReturnTypeKind is ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult
            || (entryPoint.ReturnTypeKind is ReturnTypeKind.AsyncEnumerable && aggregatorInfo.Scoped))
        {
            method = method.AddModifiers(Token(SyntaxKind.AsyncKeyword));
        }

        method = method.WithBody(Block(
            GenerateEntryPointMethodImplementationMethodBody(aggregatorInfo, entryPoint)));

        return method.NormalizeWhitespace();
    }

    private static IEnumerable<StatementSyntax> GenerateEntryPointMethodImplementationMethodBody(AggregatorInterfaceInfo aggregatorInfo, EntryPointMethodInfo entryPoint)
    {
        var serviceProviderExpression = aggregatorInfo.Scoped
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
            ReturnTypeKind.AsyncEnumerable when aggregatorInfo.Scoped =>
                // For scoped async streams, proxy enumeration via the facade interface within the scope
                // as expected by tests: scope.ServiceProvider.GetRequiredService<IFacade>().Method(...)
                ParseStatement(
                    $$"""
                    await foreach (var item in scope.ServiceProvider.GetRequiredService<{{aggregatorInfo.InterfaceSymbol.Name}}>().{{entryPoint.MethodSymbol.Name}}({{string.Join(", ", entryPoint.MethodSymbol.Parameters.Select(p => p.Name))}}))
                    {
                        yield return item;
                    }
                    """),
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

        if (!aggregatorInfo.Scoped)
        {
            yield return innerStatement;
            yield break;
        }

        // In scoped facades, wrap the call into a scope. For async streams, we already built
        // an await-foreach statement as innerStatement to proxy the stream.
        yield return entryPoint switch
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