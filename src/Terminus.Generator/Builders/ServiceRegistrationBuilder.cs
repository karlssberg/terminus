using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders;

internal static class ServiceRegistrationBuilder
{
    internal static SwitchStatementSyntax GenerateRegistrationMethodSelector(
        ImmutableArray<FacadeInterfaceInfo> facades)
    {
        var switchExpression =
            SwitchStatement(ParseExpression("typeof(T).FullName"))
                .AddSections(facades
                    .Select(facadeInfo =>
                        SwitchSection()
                            .AddLabels(
                                CaseSwitchLabel(LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(facadeInfo.InterfaceSymbol.ToDisplayString()))))
                            .AddStatements(
                                ReturnStatement(
                                    InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("services"),
                                                IdentifierName(
                                                    $"AddEntryPointFacadeFor_{facadeInfo.InterfaceSymbol.ToIdentifierString()}")))
                                        .WithArgumentList(ParseArgumentList("(configure)")))))
                    .ToArray())
                .NormalizeWhitespace();
        return switchExpression;
    }

    
    private static ExpressionStatementSyntax GenerateDispatcherServiceRegistrations(FacadeContext facadeContext)
    {
        return ExpressionStatement(
            ParseExpression(
                facadeContext.Facade.Scoped
                    ? $"services.AddTransient<ScopedDispatcher<{facadeContext.Facade.InterfaceSymbol.ToDisplayString()}>>()"
                    : $"services.AddTransient<Dispatcher<{facadeContext.Facade.InterfaceSymbol.ToDisplayString()}>>()"));
    }

    internal static SyntaxList<StatementSyntax> GenerateRegistrationsPerAttribute(
        ImmutableArray<FacadeInterfaceInfo> facades)
    {
        var registerAllEntryPoints = facades
            .Select(facadeInfo => ParseStatement(
                $"services.AddEntryPointFacadeFor_{facadeInfo.InterfaceSymbol.ToIdentifierString()}();"))
            .ToSyntaxList();
        return registerAllEntryPoints;
    }

    internal static string CreateAddEntryPointsMethods(FacadeContext facadeContext)
    {
        var facade = facadeContext.Facade;
        var facadeFullNameIdentifier = facade.InterfaceSymbol.ToIdentifierString();
        var facadeInterfaceType = facade.InterfaceSymbol.ToDisplayString();
        
        return
          $$"""
            private static IServiceCollection AddEntryPointFacadeFor_{{facadeFullNameIdentifier}}(
                this IServiceCollection services,
                Action<ParameterBindingStrategyResolver>? configure = null)
            {
                services.AddSingleton(provider =>
                {
                    var resolver = new ParameterBindingStrategyResolver(provider);
                    configure?.Invoke(resolver);
                    return resolver;
                });
                
                {{GenerateDispatcherServiceRegistrations(facadeContext)}}
                services.AddTransient<IEntryPointRouter<{{facadeInterfaceType}}>, DefaultEntryPointRouter<{{facadeInterfaceType}}>>();
                {{GenerateEntryPointDescriptorRegistrations(facadeContext)}}
                {{GenerateEntryPointContainingTypeRegistrations(facadeContext)}}
                services.AddSingleton<{{facade.InterfaceSymbol.ToDisplayString()}}, {{facade.GetImplementationClassFullName()}}>();

                return services;
            }
            """;
    }

    private static SyntaxList<StatementSyntax> GenerateEntryPointDescriptorRegistrations(FacadeContext facadeContext)
    {
        var facadeInterfaceType = facadeContext.Facade.InterfaceSymbol.ToDisplayString();
        var entryPointMethodInfos = facadeContext.EntryPointMethodInfos;
        var entryPointDescriptorRegistrations = entryPointMethodInfos
            .Select(ep =>
            {
                var attributeTypeName = ep.AttributeData.AttributeClass!.ToDisplayString();
                var methodName = ep.MethodSymbol.Name;
                var containingType = ep.MethodSymbol.ContainingType.ToDisplayString();
                var paramTypes = string.Join(", ", ep.MethodSymbol.Parameters.Select(p =>
                    $"typeof({p.Type.ToDisplayString()})"));
                var paramArray = ep.MethodSymbol.Parameters.Length == 0
                    ? "new System.Type[] { }"
                    : $"new System.Type[] {{ {paramTypes} }}";
                
                var parameterInvocations = string.Join(", ", ep.MethodSymbol.Parameters.Select(p =>
                    p.Type.ToDisplayString() == "System.Threading.CancellationToken"
                        ? "ct"
                        : $"provider.GetRequiredService<ParameterBindingStrategyResolver>().ResolveParameter<{p.Type.ToDisplayString()}>(\"{p.Name}\", context)"));

                var invokeExpressionSnippet = ep.MethodSymbol.IsStatic
                    ? $"{containingType}.{methodName}({parameterInvocations})"
                    : $"provider.GetRequiredService<{containingType}>().{methodName}({parameterInvocations})";
                    
                var invokeExpression = ParseExpression(invokeExpressionSnippet);

                var registrationExpressionStatement =
                    $"""
                     services.AddKeyedSingleton<EntryPointDescriptor<{attributeTypeName}>>(typeof({facadeInterfaceType}), (provider, key) =>
                        new EntryPointDescriptor<{attributeTypeName}>(
                            typeof({containingType}).GetMethod("{methodName}", {paramArray})!,
                            (context, ct) => {invokeExpression}));
                     """;
                return ParseStatement(registrationExpressionStatement);
            })
            .ToSyntaxList();
        return entryPointDescriptorRegistrations;
    }


    private static SyntaxList<StatementSyntax> GenerateEntryPointContainingTypeRegistrations(FacadeContext facadeContext)
    {
        return facadeContext.EntryPointMethodInfos
            .Where(ep => !ep.MethodSymbol.ContainingType.IsStatic)
            .Select(ep => ep.MethodSymbol.ContainingType.ToDisplayString())
            .Distinct()
            .Select(containingType => ParseStatement(
                $"services.AddTransient<{containingType}>();"))
            .ToSyntaxList();
    }
}