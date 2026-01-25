using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Terminus.Generator.Builders.Property;

/// <summary>
/// Builds property implementations for the facade implementation class.
/// </summary>
internal static class PropertyBuilder
{
    /// <summary>
    /// Builds an implementation property with explicit interface implementation.
    /// </summary>
    public static PropertyDeclarationSyntax BuildImplementationProperty(
        FacadeInterfaceInfo facadeInfo,
        CandidatePropertyInfo propertyInfo)
    {
        var propertySymbol = propertyInfo.PropertySymbol;
        var propertyTypeName = propertySymbol.Type.ToDisplayString(FullyQualifiedFormat);
        var interfaceName = facadeInfo.InterfaceSymbol.ToDisplayString(FullyQualifiedFormat);
        var serviceExpression = GetServiceExpression(facadeInfo, propertyInfo);

        // Build accessors string
        var accessors = BuildAccessorsString(propertyInfo, serviceExpression, propertySymbol.Name);

        // Use raw string template for proper formatting
        var propertyCode = $$"""
            {{propertyTypeName}} {{interfaceName}}.{{propertySymbol.Name}}
            {
            {{accessors}}
            }
            """;

        return (PropertyDeclarationSyntax)ParseMemberDeclaration(propertyCode)!;
    }

    private static string BuildAccessorsString(
        CandidatePropertyInfo propertyInfo,
        string serviceExpression,
        string propertyName)
    {
        var accessorLines = new List<string>();

        if (propertyInfo.HasGetter)
        {
            accessorLines.Add($"    get => {serviceExpression}.{propertyName};");
        }

        if (propertyInfo.HasSetter)
        {
            accessorLines.Add($"    set => {serviceExpression}.{propertyName} = value;");
        }

        return string.Join("\n", accessorLines);
    }

    /// <summary>
    /// Gets the service expression string for the property based on the facade configuration.
    /// </summary>
    private static string GetServiceExpression(
        FacadeInterfaceInfo facadeInfo,
        CandidatePropertyInfo propertyInfo)
    {
        var fullyQualifiedTypeName = propertyInfo.PropertySymbol.ContainingType
            .ToDisplayString(FullyQualifiedFormat);

        // Static property - direct type access
        if (propertyInfo.PropertySymbol.IsStatic)
        {
            return fullyQualifiedTypeName;
        }

        // Scoped facade - use sync scope (properties are always sync)
        if (facadeInfo.Features.IsScoped)
        {
            return $"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{fullyQualifiedTypeName}>(_syncScope.Value.ServiceProvider)";
        }

        // Non-scoped facade - use service provider directly
        return $"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{fullyQualifiedTypeName}>(_serviceProvider)";
    }
}
