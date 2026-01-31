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
internal sealed class ImplementationClassBuilder
{
    private readonly FacadeInterfaceInfo _facadeInfo;
    private readonly ImmutableArray<AggregatedMethodGroup> _methodGroups;
    private readonly ImmutableArray<CandidatePropertyInfo> _properties;
    private readonly string _interfaceName;
    private readonly string _implementationClassName;
    private readonly bool _isScoped;
    private readonly ImplementationBuildContext _context;

    private ImplementationClassBuilder(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> methodGroups,
        ImmutableArray<CandidatePropertyInfo> properties)
    {
        _facadeInfo = facadeInfo;
        _methodGroups = methodGroups;
        _properties = properties;
        _interfaceName = facadeInfo.InterfaceSymbol
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        _implementationClassName = facadeInfo.GetImplementationClassName();
        _isScoped = facadeInfo.Features.IsScoped;

        // Create build context with pre-computed values (single-pass analysis)
        _context = CreateBuildContext(facadeInfo, methodGroups, properties);
    }

    /// <summary>
    /// Builds the complete implementation class declaration.
    /// </summary>
    public static ClassDeclarationSyntax Build(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<AggregatedMethodGroup> methodGroups,
        ImmutableArray<CandidatePropertyInfo> properties = default)
    {
        var builder = new ImplementationClassBuilder(facadeInfo, methodGroups, properties);
        return builder.BuildInternal();
    }

    private ClassDeclarationSyntax BuildInternal()
    {
        // Build documentation
        var documentation = DocumentationBuilder.BuildImplementationDocumentation(_context.AllMethods, _properties);

        // Build class declaration with base type
        var classDeclaration = ClassDeclaration(_implementationClassName)
            .WithModifiers([Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword)])
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(_interfaceName)));

        if (documentation.Any())
            classDeclaration = classDeclaration.WithLeadingTrivia((IEnumerable<SyntaxTrivia>)documentation);

        // Add attributes
        classDeclaration = AddFacadeImplementationAttribute(classDeclaration);

        // Add disposal interfaces for scoped facades
        classDeclaration = AddDisposalInterfaces(classDeclaration);

        // Build all members
        var members = new List<MemberDeclarationSyntax>();

        // Fields
        members.AddRange(BuildFields());

        // Constructor
        var constructor = BuildConstructor();
        if (constructor != null)
            members.Add(constructor);

        // Methods
        members.AddRange(BuildImplementationMethods());

        // Properties
        members.AddRange(BuildProperties());

        // Disposal methods
        if (_isScoped && _context.HasInstanceMembers)
            members.AddRange(DisposalBuilder.BuildDisposalMethods());

        // Interceptor pipeline methods
        if (_context.HasInterceptors)
            members.AddRange(BuildInterceptorPipelineMethods());

        return classDeclaration.WithMembers(List(members));
    }

    private ClassDeclarationSyntax AddFacadeImplementationAttribute(ClassDeclarationSyntax classDeclaration)
    {
        var attributeLists = new List<AttributeListSyntax>();

        // Always add [GeneratedCode] attribute
        attributeLists.Add(GeneratedCodeAttributeBuilder.Build());

        // For non-scoped facades, always add [FacadeImplementation] attribute
        // For scoped facades, only add if there are instance members (static-only facades don't need it)
        if (!_isScoped || _context.HasInstanceMembers)
        {
            var facadeImplAttribute = Attribute(
                ParseName("global::Terminus.FacadeImplementation"),
                AttributeArgumentList(SingletonSeparatedList(
                    AttributeArgument(TypeOfExpression(ParseTypeName(_interfaceName))))));

            attributeLists.Add(AttributeList(SingletonSeparatedList(facadeImplAttribute)));
        }

        return classDeclaration.WithAttributeLists(List(attributeLists));
    }

    private IEnumerable<MemberDeclarationSyntax> BuildImplementationMethods()
    {
        return _methodGroups.Select(group =>
        {
            // Use the primary method for strategy determination
            var strategy = ServiceResolutionStrategyFactory.GetStrategy(_facadeInfo, group.PrimaryMethod);
            var methodBuilder = new MethodBuilder(strategy);
            return methodBuilder.BuildImplementationMethod(_facadeInfo, group);
        });
    }

    private IEnumerable<MemberDeclarationSyntax> BuildInterceptorPipelineMethods()
    {
        var requirements = _context.PipelineRequirements;

        // Check if we should use generic pipeline methods
        var isGenericFacade = _facadeInfo.IsGenericFacade;
        var attributeTypeName = isGenericFacade && _facadeInfo.FacadeMethodAttributeTypes.Length > 0
            ? _facadeInfo.FacadeMethodAttributeTypes[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
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

    private ClassDeclarationSyntax AddDisposalInterfaces(ClassDeclarationSyntax classDeclaration)
    {
        if (_isScoped && _context.HasInstanceMembers)
        {
            return classDeclaration.AddBaseListTypes(
                SimpleBaseType(ParseTypeName("global::System.IDisposable")),
                SimpleBaseType(ParseTypeName("global::System.IAsyncDisposable")));
        }

        return classDeclaration;
    }

    private IEnumerable<MemberDeclarationSyntax> BuildFields()
    {
        if (_isScoped && _context.HasInstanceMembers)
        {
            foreach (var field in FieldBuilder.BuildScopedFields())
                yield return field;
        }
        else if (!_isScoped)
        {
            foreach (var field in FieldBuilder.BuildNonScopedFields())
                yield return field;
        }

        if (_context.HasInterceptors)
            yield return FieldBuilder.BuildInterceptorsField();
    }

    private MemberDeclarationSyntax? BuildConstructor()
    {
        // Scoped facades without instance members don't need a constructor
        if (_isScoped && !_context.HasInstanceMembers)
            return null;

        var hasInterceptors = _context.HasInterceptors;
        var interceptorTypes = _facadeInfo.Features.InterceptorTypes;

        return (_isScoped, hasInterceptors) switch
        {
            (true, true) => ConstructorBuilder.BuildScopedConstructorWithInterceptors(
                _implementationClassName, interceptorTypes),
            (true, false) => ConstructorBuilder.BuildScopedConstructor(_implementationClassName),
            (false, true) => ConstructorBuilder.BuildNonScopedConstructorWithInterceptors(
                _implementationClassName, interceptorTypes),
            (false, false) => ConstructorBuilder.BuildNonScopedConstructor(_implementationClassName),
        };
    }

    private IEnumerable<MemberDeclarationSyntax> BuildProperties()
    {
        if (_properties.IsDefault || _properties.IsEmpty)
            yield break;

        foreach (var prop in _properties.OrderBy(p => p.PropertySymbol.Name))
            yield return PropertyBuilder.BuildImplementationProperty(_facadeInfo, prop);
    }
}
