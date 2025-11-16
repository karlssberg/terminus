using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders;

internal static class ServiceRegistrationBuilder
{
    internal static SwitchStatementSyntax GenerateRegistrationMethodSelector(
        ImmutableArray<AggregatorInterfaceInfo> facades)
    {
        var switchExpression =
            SwitchStatement(ParseExpression("typeof(T).FullName"))
                .AddSections(facades
                    .Select(aggregatorInfo =>
                        SwitchSection()
                            .AddLabels(
                                CaseSwitchLabel(LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(aggregatorInfo.InterfaceSymbol.ToDisplayString()))))
                            .AddStatements(
                                ReturnStatement(
                                    InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("services"),
                                                IdentifierName(
                                                    $"AddEntryPointsFor_{aggregatorInfo.InterfaceSymbol.ToIdentifierString()}")))
                                        .WithArgumentList(ParseArgumentList("(configure)")))))
                    .ToArray())
                .NormalizeWhitespace();
        return switchExpression;
    }

    
    private static ExpressionStatementSyntax GenerateDispatcherServiceRegistrations(AggregatorContext aggregatorContext)
    {
        return ExpressionStatement(
            ParseExpression(
                aggregatorContext.Aggregator.Scoped
                    ? $"services.AddTransient<ScopedDispatcher<{aggregatorContext.Aggregator.InterfaceSymbol.ToDisplayString()}>>()"
                    : $"services.AddTransient<Dispatcher<{aggregatorContext.Aggregator.InterfaceSymbol.ToDisplayString()}>>()"));
    }

    internal static SyntaxList<StatementSyntax> GenerateRegistrationsPerAttribute(
        ImmutableArray<AggregatorInterfaceInfo> facades)
    {
        var registerAllEntryPoints = facades
            .Select(aggregatorInfo => ParseStatement(
                $"services.AddEntryPointsFor_{aggregatorInfo.InterfaceSymbol.ToIdentifierString()}();"))
            .ToSyntaxList();
        return registerAllEntryPoints;
    }

    internal static string CreateAddEntryPointsMethods(AggregatorContext aggregatorContext)
    {
        var facade = aggregatorContext.Aggregator;
        var facadeFullNameIdentifier = facade.InterfaceSymbol.ToIdentifierString();
        var facadeInterfaceType = facade.InterfaceSymbol.ToDisplayString();
        
        return
          $$"""
            private static IServiceCollection AddEntryPointsFor_{{facadeFullNameIdentifier}}(
                this IServiceCollection services,
                Action<ParameterBindingStrategyResolver>? configure = null)
            {
                services.AddSingleton(provider =>
                {
                    var resolver = new ParameterBindingStrategyResolver(provider);
                    configure?.Invoke(resolver);
                    return resolver;
                });
                
                {{GenerateDispatcherServiceRegistrations(aggregatorContext)}}
                services.AddTransient<IEntryPointRouter<{{facadeInterfaceType}}>, DefaultEntryPointRouter<{{facadeInterfaceType}}>>();
                {{GenerateEntryPointDescriptorRegistrations(aggregatorContext)}}
                {{GenerateEntryPointContainingTypeRegistrations(aggregatorContext)}}
                services.AddSingleton<{{facade.InterfaceSymbol.ToDisplayString()}}, {{facade.GetImplementationClassFullName()}}>();

                return services;
            }
            """;
    }

    private static SyntaxList<StatementSyntax> GenerateEntryPointDescriptorRegistrations(AggregatorContext aggregatorContext)
    {
        var facadeInterfaceType = aggregatorContext.Aggregator.InterfaceSymbol.ToDisplayString();
        var entryPointMethodInfos = aggregatorContext.EntryPointMethodInfos;
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


    private static SyntaxList<StatementSyntax> GenerateEntryPointContainingTypeRegistrations(AggregatorContext aggregatorContext)
    {
        return aggregatorContext.EntryPointMethodInfos
            .Where(ep => !ep.MethodSymbol.ContainingType.IsStatic)
            .Select(ep => ep.MethodSymbol.ContainingType.ToDisplayString())
            .Distinct()
            .Select(containingType => ParseStatement(
                $"services.AddTransient<{containingType}>();"))
            .ToSyntaxList();
    }
}