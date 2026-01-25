using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Attributes;
using Terminus.Generator.Builders.Documentation;
using Terminus.Generator.Builders.Method;
using Terminus.Generator.Builders.Property;
using Terminus.Generator.Builders.Strategies;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Class;

/// <summary>
/// Orchestrates the building of the complete facade implementation class.
/// </summary>
internal static class ImplementationClassBuilder
{
    /// <summary>
    /// Builds the complete implementation class declaration.
    /// </summary>
    public static ClassDeclarationSyntax Build(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> methodGroups,
        ImmutableArray<CandidatePropertyInfo> properties = default)
    {
        var interfaceName = facadeInfo.InterfaceSymbol
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var implementationClassName = facadeInfo.GetImplementationClassName();

        // Flatten all methods from groups for documentation and analysis
        var allMethods = methodGroups.SelectMany(g => g.Methods).ToImmutableArray();
        var documentation = DocumentationBuilder.BuildImplementationDocumentation(allMethods, properties);

        var classDeclaration = ClassDeclaration(implementationClassName)
            .WithModifiers([Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(interfaceName)));

        if (documentation.Any())
        {
            classDeclaration = classDeclaration.WithLeadingTrivia((IEnumerable<SyntaxTrivia>)documentation);
        }

        // Determine if we need IServiceProvider based on whether we have instance members (methods or properties)
        var hasInstanceMethods = allMethods.Any(m => !m.MethodSymbol.IsStatic);
        var hasInstanceProperties = !properties.IsDefault && properties.Any(p => !p.PropertySymbol.IsStatic);
        var hasInstanceMembers = hasInstanceMethods || hasInstanceProperties;

        // Check for interceptors
        var hasInterceptors = facadeInfo.Features.HasInterceptors;
        var interceptorTypes = facadeInfo.Features.InterceptorTypes;

        // Add [FacadeImplementation] attribute
        classDeclaration = AddFacadeImplementationAttribute(
            classDeclaration,
            interfaceName,
            facadeInfo.Features.IsScoped,
            hasInstanceMembers);

        // Add disposal interfaces for scoped facades with instance members
        if (facadeInfo.Features.IsScoped && hasInstanceMembers)
        {
            classDeclaration = classDeclaration.AddBaseListTypes(
                SimpleBaseType(ParseTypeName("global::System.IDisposable")),
                SimpleBaseType(ParseTypeName("global::System.IAsyncDisposable")));
        }

        // Build members
        var members = new List<MemberDeclarationSyntax>();

        // Add fields
        if (facadeInfo.Features.IsScoped && hasInstanceMembers)
            members.AddRange(FieldBuilder.BuildScopedFields());
        else if (!facadeInfo.Features.IsScoped)
            members.AddRange(FieldBuilder.BuildNonScopedFields());

        // Add interceptors field if needed
        if (hasInterceptors)
            members.Add(FieldBuilder.BuildInterceptorsField());

        // Add constructor
        if (hasInterceptors)
        {
            if (facadeInfo.Features.IsScoped && hasInstanceMembers)
                members.Add(ConstructorBuilder.BuildScopedConstructorWithInterceptors(implementationClassName, interceptorTypes));
            else if (!facadeInfo.Features.IsScoped)
                members.Add(ConstructorBuilder.BuildNonScopedConstructorWithInterceptors(implementationClassName, interceptorTypes));
        }
        else
        {
            if (facadeInfo.Features.IsScoped && hasInstanceMembers)
                members.Add(ConstructorBuilder.BuildScopedConstructor(implementationClassName));
            else if (!facadeInfo.Features.IsScoped)
                members.Add(ConstructorBuilder.BuildNonScopedConstructor(implementationClassName));
        }

        // Add implementation methods
        members.AddRange(BuildImplementationMethods(facadeInfo, methodGroups));

        // Add implementation properties (sorted by name for consistent output)
        if (!properties.IsDefault && !properties.IsEmpty)
        {
            var sortedProperties = properties.OrderBy(p => p.PropertySymbol.Name).ToList();
            members.AddRange(sortedProperties.Select(prop => PropertyBuilder.BuildImplementationProperty(facadeInfo, prop)));
        }

        // Add disposal methods for scoped facades
        if (facadeInfo.Features.IsScoped && hasInstanceMembers)
        {
            members.AddRange(DisposalBuilder.BuildDisposalMethods());
        }

        // Add interceptor pipeline methods if needed
        if (hasInterceptors)
        {
            members.AddRange(BuildInterceptorPipelineMethods(methodGroups));
        }

        return classDeclaration.WithMembers(List(members));
    }

    private static ClassDeclarationSyntax AddFacadeImplementationAttribute(
        ClassDeclarationSyntax classDeclaration,
        string interfaceName,
        bool isScoped,
        bool hasInstanceMembers)
    {
        var attributeLists = new List<AttributeListSyntax>();

        // Always add [GeneratedCode] attribute
        attributeLists.Add(GeneratedCodeAttributeBuilder.Build());

        // For non-scoped facades, always add [FacadeImplementation] attribute
        // For scoped facades, only add if there are instance members (static-only facades don't need it)
        if (!isScoped || hasInstanceMembers)
        {
            var facadeImplAttribute = Attribute(
                ParseName("global::Terminus.FacadeImplementation"),
                AttributeArgumentList(SingletonSeparatedList(
                    AttributeArgument(TypeOfExpression(ParseTypeName(interfaceName))))));

            attributeLists.Add(AttributeList(SingletonSeparatedList(facadeImplAttribute)));
        }

        classDeclaration = classDeclaration.WithAttributeLists(List(attributeLists));

        return classDeclaration;
    }

    private static IEnumerable<MemberDeclarationSyntax> BuildImplementationMethods(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> methodGroups)
    {
        return methodGroups.Select(group =>
        {
            // Use the primary method for strategy determination
            var strategy = ServiceResolutionStrategyFactory.GetStrategy(facadeInfo, group.PrimaryMethod);
            var methodBuilder = new MethodBuilder(strategy);
            return methodBuilder.BuildImplementationMethod(facadeInfo, group);
        });
    }

    private static IEnumerable<MemberDeclarationSyntax> BuildInterceptorPipelineMethods(
        ImmutableArray<AggregatedMethodGroup> methodGroups)
    {
        var allMethods = methodGroups.SelectMany(g => g.Methods).ToList();

        // Determine which pipeline methods are needed based on return types
        var needsSyncVoid = allMethods.Any(m => m.ReturnTypeKind is ReturnTypeKind.Void);
        var needsSyncResult = allMethods.Any(m => m.ReturnTypeKind is ReturnTypeKind.Result);
        var needsAsyncVoid = allMethods.Any(m =>
            m.ReturnTypeKind is ReturnTypeKind.Task or ReturnTypeKind.ValueTask);
        var needsAsyncResult = allMethods.Any(m =>
            m.ReturnTypeKind is ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult);
        var needsStream = allMethods.Any(m =>
            m.ReturnTypeKind is ReturnTypeKind.AsyncEnumerable);

        if (needsSyncVoid)
            yield return InterceptorPipelineBuilder.BuildSyncVoidPipelineMethod();

        if (needsSyncResult)
            yield return InterceptorPipelineBuilder.BuildSyncPipelineMethod();

        if (needsAsyncVoid)
            yield return InterceptorPipelineBuilder.BuildAsyncVoidPipelineMethod();

        if (needsAsyncResult)
            yield return InterceptorPipelineBuilder.BuildAsyncPipelineMethod();

        if (needsStream)
            yield return InterceptorPipelineBuilder.BuildStreamPipelineMethod();
    }
}
