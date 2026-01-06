using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders;

internal static class FacadeBuilder
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
            .WithModifiers(TokenList(
                Token(SyntaxKind.PublicKeyword), 
                Token(SyntaxKind.PartialKeyword)))
            .WithMembers(aggregatorContext.FacadeMethodMethodInfos.Select(GenerateFacadeMethodMethodInterfaceDefinition).ToSyntaxList())
            .NormalizeWhitespace();
    }

    private static ClassDeclarationSyntax GenerateAggregatorClassImplementationWithScope(AggregatorContext aggregatorContext)
    {
        var interfaceName = aggregatorContext.Facade.InterfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var implementationClassName = aggregatorContext.Facade.GetImplementationClassName();

        var classDeclaration = ClassDeclaration(implementationClassName)
            .WithModifiers([Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(interfaceName)));

        // Determine if we need IServiceProvider based on whether we have instance methods
        var hasInstanceMethods = aggregatorContext.FacadeMethodMethodInfos.Any(m => !m.MethodSymbol.IsStatic);

        // For non-scoped facades, always add [FacadeImplementation] attribute
        // For scoped facades, only add if there are instance methods (static-only facades don't need it)
        if (!aggregatorContext.Facade.Scoped || hasInstanceMethods)
        {
            var facadeImplAttribute = Attribute(
                ParseName("global::Terminus.FacadeImplementation"),
                AttributeArgumentList(SingletonSeparatedList(
                    AttributeArgument(TypeOfExpression(ParseTypeName(interfaceName))))));

            classDeclaration = classDeclaration.WithAttributeLists(
                SingletonList(AttributeList(SingletonSeparatedList(facadeImplAttribute))));
        }

        var members = new List<MemberDeclarationSyntax>();

        // Add IServiceProvider field only for non-scoped facades
        if (!aggregatorContext.Facade.Scoped)
        {
            members.Add(ParseMemberDeclaration("private readonly global::System.IServiceProvider _serviceProvider;")!);
        }

        // For scoped facades with instance methods, add lazy scope fields and disposable interfaces
        if (aggregatorContext.Facade.Scoped && hasInstanceMethods)
        {
            members.Add(ParseMemberDeclaration("private bool _syncDisposed;")!);
            members.Add(ParseMemberDeclaration("private bool _asyncDisposed;")!);
            members.Add(ParseMemberDeclaration("private readonly global::System.Lazy<global::Microsoft.Extensions.DependencyInjection.IServiceScope> _syncScope;")!);
            members.Add(ParseMemberDeclaration("private readonly global::System.Lazy<global::Microsoft.Extensions.DependencyInjection.AsyncServiceScope> _asyncScope;")!);

            // Add IDisposable and IAsyncDisposable to base list
            classDeclaration = classDeclaration.AddBaseListTypes(
                SimpleBaseType(ParseTypeName("global::System.IDisposable")),
                SimpleBaseType(ParseTypeName("global::System.IAsyncDisposable")));

            members.Add(ParseMemberDeclaration(
                $$"""
                  public {{implementationClassName}}(global::System.IServiceProvider serviceProvider)
                  {
                      _syncScope = new global::System.Lazy<global::Microsoft.Extensions.DependencyInjection.IServiceScope>(() => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>(serviceProvider).CreateScope());
                      _asyncScope = new global::System.Lazy<global::Microsoft.Extensions.DependencyInjection.AsyncServiceScope>(() => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateAsyncScope(serviceProvider));
                  }
                  """)!);
        }
        else if (!aggregatorContext.Facade.Scoped)
        {
            // Non-scoped facades: always add constructor
            members.Add(ParseMemberDeclaration(
                $$"""
                  public {{implementationClassName}}(global::System.IServiceProvider serviceProvider)
                  {
                      _serviceProvider = serviceProvider;
                  }
                  """)!);
        }

        members.AddRange(GenerateImplementationFacadeMethods(aggregatorContext));

        // Add Dispose methods for scoped facades
        if (aggregatorContext.Facade.Scoped && hasInstanceMethods)
        {
            members.Add(ParseMemberDeclaration(
                """
                public void Dispose()
                {
                    if (_syncDisposed || !_syncScope.IsValueCreated) return;

                    _syncScope.Value.Dispose();
                    _syncDisposed = true;

                    global::System.GC.SuppressFinalize(this);
                }
                """)!);

            members.Add(ParseMemberDeclaration(
                """
                public async global::System.Threading.Tasks.ValueTask DisposeAsync()
                {
                    if (_asyncDisposed || !_asyncScope.IsValueCreated) return;

                    await _asyncScope.Value.DisposeAsync().ConfigureAwait(false);
                    _asyncDisposed = true;

                    global::System.GC.SuppressFinalize(this);
                }
                """)!);
        }

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

        // Use explicit interface implementation
        var interfaceName = facadeInfo.InterfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var explicitInterfaceSpecifier = ExplicitInterfaceSpecifier(ParseName(interfaceName));

        var method = MethodDeclaration(returnTypeSyntax, Identifier(candidate.MethodSymbol.Name))
            .WithExplicitInterfaceSpecifier(explicitInterfaceSpecifier)
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
        // Handle CancellationToken.ThrowIfCancellationRequested() first for static scoped methods
        var cancellationTokens = candidate.MethodSymbol.Parameters.Where(p =>
            !p.IsParams && p.Type.ToDisplayString() == typeof(CancellationToken).FullName)
            .ToList();

        if (cancellationTokens.Count == 1 && candidate.MethodSymbol.IsStatic && facadeInfo.Scoped)
        {
            var parameterName = cancellationTokens[0].Name;
            yield return ParseStatement($"{parameterName}.ThrowIfCancellationRequested();");
        }

        // Build instance/service resolution expression
        var fullyQualifiedTypeName = candidate.MethodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        // For static methods, use the fully qualified type name
        // For instance methods in scoped facades, use the appropriate scope's ServiceProvider
        // For instance methods in non-scoped facades, use _serviceProvider
        ExpressionSyntax instanceExpression;
        if (candidate.MethodSymbol.IsStatic)
        {
            instanceExpression = ParseExpression(fullyQualifiedTypeName);
        }
        else if (facadeInfo.Scoped)
        {
            // For scoped facades, determine which scope to use based on method return type
            var scopeExpression = candidate.ReturnTypeKind is ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult or ReturnTypeKind.AsyncEnumerable
                ? "_asyncScope.Value.ServiceProvider"
                : "_syncScope.Value.ServiceProvider";
            instanceExpression = ParseExpression($"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{fullyQualifiedTypeName}>({scopeExpression})");
        }
        else
        {
            instanceExpression = ParseExpression($"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{fullyQualifiedTypeName}>(_serviceProvider)");
        }

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
                ParseStatement(
                    $$"""
                    await foreach (var item in global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{{facadeInfo.InterfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>(_asyncScope.Value.ServiceProvider).{{candidate.MethodSymbol.Name}}({{string.Join(", ", candidate.MethodSymbol.Parameters.Select(p => p.Name))}}))
                    {
                        yield return item;
                    }
                    """),
            _ => ReturnStatement(invocationExpression)
        };

        yield return innerStatement;
    }

}