using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terminus.Generator.Builders;

internal static class ServiceRegistrationBuilder
{
    internal static SwitchStatementSyntax GenerateRegistrationMethodSelector(
        ImmutableArray<INamedTypeSymbol> entryPointAttributeTypes)
    {
        var switchExpression =
            SyntaxFactory.SwitchStatement(SyntaxFactory.ParseExpression("typeof(T).FullName"))
                .AddSections(entryPointAttributeTypes
                    .Select(attributeType =>
                        SyntaxFactory.SwitchSection()
                            .AddLabels(
                                SyntaxFactory.CaseSwitchLabel(SyntaxFactory.LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal(attributeType.ToDisplayString()))))
                            .AddStatements(
                                SyntaxFactory.ReturnStatement(
                                    SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName("services"),
                                                SyntaxFactory.IdentifierName(
                                                    $"AddEntryPointsFor_{attributeType.ToIdentifierString()}")))
                                        .WithArgumentList(SyntaxFactory.ParseArgumentList("(configure)")))))
                    .ToArray())
                .NormalizeWhitespace();
        return switchExpression;
    }

    internal static SyntaxList<StatementSyntax> GenerateRegistrationsPerAttribute(ImmutableArray<INamedTypeSymbol> entryPointAttributeTypes)
    {
        var registerAllEntryPoints = entryPointAttributeTypes
            .Select(attributeType => SyntaxFactory.ParseStatement(
                $"services.AddEntryPointsFor_{attributeType.ToIdentifierString()}();"))
            .ToSyntaxList();
        return registerAllEntryPoints;
    }

    internal static string CreateAddEntryPointsMethods(
        INamedTypeSymbol entryPointAttributeType,
        ImmutableArray<FacadeInterfaceInfo> facades,
        ImmutableArray<EntryPointMethodInfo> entryPointMethodInfos)
    {
        var entryPointAttributeTypeDisplay = entryPointAttributeType.ToDisplayString();

        return
          $$"""
            private static IServiceCollection AddEntryPointsFor_{{entryPointAttributeType.ToIdentifierString()}}(
                this IServiceCollection services,
                Action<ParameterBindingStrategyResolver>? configure = null)
            {
                services.AddSingleton(provider =>
                {
                    var resolver = new ParameterBindingStrategyResolver(provider);
                    configure?.Invoke(resolver);
                    return resolver;
                });
                services.AddTransient<ScopedDispatcher<{{entryPointAttributeTypeDisplay}}>>();
                services.AddTransient<Dispatcher<{{entryPointAttributeTypeDisplay}}>>();
                services.AddTransient<IEntryPointRouter<{{entryPointAttributeTypeDisplay}}>, DefaultEntryPointRouter<{{entryPointAttributeTypeDisplay}}>>();

                {{GenerateEntryPointDescriptorRegistrations(entryPointMethodInfos, entryPointAttributeTypeDisplay)}}
                
                {{GenerateEntryPointContainingTypeRegistrations(entryPointMethodInfos)}}

                {{GenerateFacadeServiceRegistrations(facades)}}

                return services;
            }
            """;
    }

    private static SyntaxList<StatementSyntax> GenerateEntryPointDescriptorRegistrations(ImmutableArray<EntryPointMethodInfo> entryPointMethodInfos,
        string attributeTypeName)
    {
        var entryPointDescriptorRegistrations = entryPointMethodInfos
            .Select(ep =>
            {
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
                    
                var invokeExpression = SyntaxFactory.ParseExpression(invokeExpressionSnippet);

                var registrationExpressionStatement =
                    $"""
                     services.AddSingleton<EntryPointDescriptor<{attributeTypeName}>>(provider =>
                        new EntryPointDescriptor<{attributeTypeName}>(
                            typeof({containingType}).GetMethod("{methodName}", {paramArray})!,
                            (context, ct) => {invokeExpression}));
                     """;
                return SyntaxFactory.ParseStatement(registrationExpressionStatement);
            })
            .ToSyntaxList();
        return entryPointDescriptorRegistrations;
    }

    private static SyntaxList<StatementSyntax> GenerateEntryPointContainingTypeRegistrations(ImmutableArray<EntryPointMethodInfo> entryPointMethodInfos)
    {
        return entryPointMethodInfos
            .Where(ep => !ep.MethodSymbol.ContainingType.IsStatic)
            .Select(ep => ep.MethodSymbol.ContainingType.ToDisplayString())
            .Distinct()
            .Select(containingType => SyntaxFactory.ParseStatement(
                $"services.AddTransient<{containingType}>();"))
            .ToSyntaxList();
    }

    private static SyntaxList<ExpressionStatementSyntax> GenerateFacadeServiceRegistrations(ImmutableArray<FacadeInterfaceInfo> facades)
    {
        return facades
            .Select(facade => SyntaxFactory.ParseExpression(
                $"services.AddSingleton<{facade.InterfaceSymbol.ToDisplayString()}, {facade.GetImplementationClassFullName()}>()"))
            .Select(SyntaxFactory.ExpressionStatement)
            .ToSyntaxList();
    }
}