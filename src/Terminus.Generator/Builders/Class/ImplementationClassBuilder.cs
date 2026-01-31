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
/// Captures pre-computed requirements for interceptor pipeline method generation.
/// Computed once to avoid multiple passes over method collections.
/// </summary>
internal readonly record struct InterceptorPipelineRequirements(
    bool NeedsSyncVoid,
    bool NeedsSyncResult,
    bool NeedsAsyncVoid,
    bool NeedsAsyncResult,
    bool NeedsStream);

/// <summary>
/// Holds pre-computed values for implementation class building.
/// This context is created once and reused throughout the build process.
/// </summary>
internal readonly record struct ImplementationBuildContext(
    ImmutableArray<CandidateMethodInfo> AllMethods,
    bool HasInstanceMembers,
    bool HasInterceptors,
    InterceptorPipelineRequirements PipelineRequirements);

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
        var isScoped = facadeInfo.Features.IsScoped;

        // Create build context with pre-computed values (single-pass analysis)
        var context = CreateBuildContext(facadeInfo, methodGroups, properties);

        // Build documentation
        var documentation = DocumentationBuilder.BuildImplementationDocumentation(context.AllMethods, properties);

        // Build class declaration with base type
        var classDeclaration = ClassDeclaration(implementationClassName)
            .WithModifiers([Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(interfaceName)));

        if (documentation.Any())
            classDeclaration = classDeclaration.WithLeadingTrivia((IEnumerable<SyntaxTrivia>)documentation);

        // Add attributes
        classDeclaration = AddFacadeImplementationAttribute(
            classDeclaration, interfaceName, isScoped, context.HasInstanceMembers);

        // Add disposal interfaces for scoped facades
        classDeclaration = AddDisposalInterfaces(classDeclaration, isScoped, context.HasInstanceMembers);

        // Build all members
        var members = new List<MemberDeclarationSyntax>();

        // Fields
        members.AddRange(BuildFields(isScoped, context.HasInstanceMembers, context.HasInterceptors));

        // Constructor
        var constructor = SelectConstructor(
            implementationClassName,
            isScoped,
            context.HasInstanceMembers,
            context.HasInterceptors,
            facadeInfo.Features.InterceptorTypes);

        if (constructor != null)
            members.Add(constructor);

        // Methods
        members.AddRange(BuildImplementationMethods(facadeInfo, methodGroups));

        // Properties
        members.AddRange(BuildProperties(facadeInfo, properties));

        // Disposal methods
        if (isScoped && context.HasInstanceMembers)
            members.AddRange(DisposalBuilder.BuildDisposalMethods());

        // Interceptor pipeline methods
        if (context.HasInterceptors)
            members.AddRange(BuildInterceptorPipelineMethods(facadeInfo, context.PipelineRequirements));

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
        FacadeInterfaceInfo facadeInfo,
        InterceptorPipelineRequirements requirements)
    {
        // Check if we should use generic pipeline methods
        var isGenericFacade = facadeInfo.IsGenericFacade;
        var attributeTypeName = isGenericFacade && facadeInfo.FacadeMethodAttributeTypes.Length > 0
            ? facadeInfo.FacadeMethodAttributeTypes[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : null;

        if (isGenericFacade && attributeTypeName is not null)
        {
            // Use generic pipeline methods
            if (requirements.NeedsSyncVoid)
                yield return InterceptorPipelineBuilder.BuildGenericSyncVoidPipelineMethod(attributeTypeName);

            if (requirements.NeedsSyncResult)
                yield return InterceptorPipelineBuilder.BuildGenericSyncPipelineMethod(attributeTypeName);

            if (requirements.NeedsAsyncVoid)
                yield return InterceptorPipelineBuilder.BuildGenericAsyncVoidPipelineMethod(attributeTypeName);

            if (requirements.NeedsAsyncResult)
                yield return InterceptorPipelineBuilder.BuildGenericAsyncPipelineMethod(attributeTypeName);

            if (requirements.NeedsStream)
                yield return InterceptorPipelineBuilder.BuildGenericStreamPipelineMethod(attributeTypeName);
        }
        else
        {
            // Use non-generic pipeline methods
            if (requirements.NeedsSyncVoid)
                yield return InterceptorPipelineBuilder.BuildSyncVoidPipelineMethod();

            if (requirements.NeedsSyncResult)
                yield return InterceptorPipelineBuilder.BuildSyncPipelineMethod();

            if (requirements.NeedsAsyncVoid)
                yield return InterceptorPipelineBuilder.BuildAsyncVoidPipelineMethod();

            if (requirements.NeedsAsyncResult)
                yield return InterceptorPipelineBuilder.BuildAsyncPipelineMethod();

            if (requirements.NeedsStream)
                yield return InterceptorPipelineBuilder.BuildStreamPipelineMethod();
        }
    }

    /// <summary>
    /// Creates a build context with pre-computed values for efficient class building.
    /// Performs single-pass analysis over methods and properties.
    /// </summary>
    private static ImplementationBuildContext CreateBuildContext(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> methodGroups,
        ImmutableArray<CandidatePropertyInfo> properties)
    {
        var allMethods = methodGroups.SelectMany(g => g.Methods).ToImmutableArray();
        var hasInterceptors = facadeInfo.Features.HasInterceptors;

        // Single-pass analysis for instance members and pipeline requirements
        var hasInstanceMethods = false;
        var needsSyncVoid = false;
        var needsSyncResult = false;
        var needsAsyncVoid = false;
        var needsAsyncResult = false;
        var needsStream = false;

        foreach (var method in allMethods)
        {
            if (!method.MethodSymbol.IsStatic)
                hasInstanceMethods = true;

            // Only compute pipeline requirements if we have interceptors
            if (hasInterceptors)
            {
                switch (method.ReturnTypeKind)
                {
                    case ReturnTypeKind.Void:
                        needsSyncVoid = true;
                        break;
                    case ReturnTypeKind.Result:
                        needsSyncResult = true;
                        break;
                    case ReturnTypeKind.Task:
                    case ReturnTypeKind.ValueTask:
                        needsAsyncVoid = true;
                        break;
                    case ReturnTypeKind.TaskWithResult:
                    case ReturnTypeKind.ValueTaskWithResult:
                        needsAsyncResult = true;
                        break;
                    case ReturnTypeKind.AsyncEnumerable:
                        needsStream = true;
                        break;
                }
            }
        }

        var hasInstanceProperties = !properties.IsDefault && properties.Any(p => !p.PropertySymbol.IsStatic);
        var hasInstanceMembers = hasInstanceMethods || hasInstanceProperties;

        var pipelineRequirements = new InterceptorPipelineRequirements(
            needsSyncVoid,
            needsSyncResult,
            needsAsyncVoid,
            needsAsyncResult,
            needsStream);

        return new ImplementationBuildContext(
            allMethods,
            hasInstanceMembers,
            hasInterceptors,
            pipelineRequirements);
    }

    /// <summary>
    /// Adds disposal interfaces to the class declaration for scoped facades with instance members.
    /// </summary>
    private static ClassDeclarationSyntax AddDisposalInterfaces(
        ClassDeclarationSyntax classDeclaration,
        bool isScoped,
        bool hasInstanceMembers)
    {
        if (isScoped && hasInstanceMembers)
        {
            return classDeclaration.AddBaseListTypes(
                SimpleBaseType(ParseTypeName("global::System.IDisposable")),
                SimpleBaseType(ParseTypeName("global::System.IAsyncDisposable")));
        }

        return classDeclaration;
    }

    /// <summary>
    /// Builds field declarations based on scope and interceptor configuration.
    /// </summary>
    private static IEnumerable<MemberDeclarationSyntax> BuildFields(
        bool isScoped,
        bool hasInstanceMembers,
        bool hasInterceptors)
    {
        if (isScoped && hasInstanceMembers)
        {
            foreach (var field in FieldBuilder.BuildScopedFields())
                yield return field;
        }
        else if (!isScoped)
        {
            foreach (var field in FieldBuilder.BuildNonScopedFields())
                yield return field;
        }

        if (hasInterceptors)
            yield return FieldBuilder.BuildInterceptorsField();
    }

    /// <summary>
    /// Selects and builds the appropriate constructor based on scope and interceptor configuration.
    /// </summary>
    private static MemberDeclarationSyntax? SelectConstructor(
        string implementationClassName,
        bool isScoped,
        bool hasInstanceMembers,
        bool hasInterceptors,
        ImmutableArray<INamedTypeSymbol> interceptorTypes)
    {
        // Scoped facades without instance members don't need a constructor
        if (isScoped && !hasInstanceMembers)
            return null;

        return (isScoped, hasInterceptors) switch
        {
            (true, true) => ConstructorBuilder.BuildScopedConstructorWithInterceptors(
                implementationClassName, interceptorTypes),
            (true, false) => ConstructorBuilder.BuildScopedConstructor(implementationClassName),
            (false, true) => ConstructorBuilder.BuildNonScopedConstructorWithInterceptors(
                implementationClassName, interceptorTypes),
            (false, false) => ConstructorBuilder.BuildNonScopedConstructor(implementationClassName),
        };
    }

    /// <summary>
    /// Builds property implementations sorted by name for consistent output.
    /// </summary>
    private static IEnumerable<MemberDeclarationSyntax> BuildProperties(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<CandidatePropertyInfo> properties)
    {
        if (properties.IsDefault || properties.IsEmpty)
            yield break;

        foreach (var prop in properties.OrderBy(p => p.PropertySymbol.Name))
            yield return PropertyBuilder.BuildImplementationProperty(facadeInfo, prop);
    }
}
