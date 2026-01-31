using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Terminus.Generator.Discovery;

/// <summary>
/// Discovers open generic types with facade method attributes and finds their closed generic instantiations.
/// </summary>
internal static class OpenGenericMethodDiscovery
{
    /// <summary>
    /// Discovers all closed generic instantiations of open generic types that have facade method attributes.
    /// </summary>
    /// <param name="compilation">The current compilation.</param>
    /// <param name="facadeMethodAttributeTypes">The attribute types that identify facade methods.</param>
    /// <param name="context">The source production context for reporting diagnostics.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of candidate method info for all closed generic instantiations.</returns>
    public static ImmutableArray<CandidateMethodInfo> DiscoverClosedGenericMethods(
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> facadeMethodAttributeTypes,
        SourceProductionContext context,
        CancellationToken ct)
    {
        // Step 1: Find all open generic types with facade method attributes (class-level or method-level)
        var openGenericTypes = DiscoverOpenGenericTypes(compilation, facadeMethodAttributeTypes, ct);

        if (openGenericTypes.IsEmpty)
            return ImmutableArray<CandidateMethodInfo>.Empty;

        // Step 2: Find all closed generic instantiations of these types
        var closedInstantiations = DiscoverClosedGenericInstantiations(compilation, openGenericTypes, ct);

        // Step 3: Report warning for open generic types without closed instantiations
        ReportMissingInstantiations(context, openGenericTypes, closedInstantiations);

        if (closedInstantiations.IsEmpty)
            return ImmutableArray<CandidateMethodInfo>.Empty;

        // Step 4: Expand closed generics into CandidateMethodInfo entries
        return ExpandClosedGenericsToMethods(compilation, closedInstantiations, facadeMethodAttributeTypes, ct);
    }

    /// <summary>
    /// Discovers open generic types that have facade method attributes.
    /// </summary>
    private static ImmutableArray<OpenGenericTypeInfo> DiscoverOpenGenericTypes(
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> facadeMethodAttributeTypes,
        CancellationToken ct)
    {
        var result = ImmutableArray.CreateBuilder<OpenGenericTypeInfo>();

        foreach (var type in GetAllTypes(compilation.Assembly.GlobalNamespace, ct))
        {
            ct.ThrowIfCancellationRequested();

            // Only process open generic types (types with unspecified type parameters)
            if (!type.IsGenericType || type.IsUnboundGenericType)
                continue;

            // Check if this is an open generic (has type parameters, not type arguments)
            var isOpenGeneric = type.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);
            if (!isOpenGeneric)
                continue;

            // Check for class-level attributes
            var classLevelAttributes = type.GetAttributes()
                .Where(attr => attr.AttributeClass is not null &&
                               InheritsFromAnyAttribute(attr.AttributeClass, facadeMethodAttributeTypes))
                .ToArray();

            // Check for method-level attributes
            var methodsWithAttributes = type.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m =>
                    m.DeclaredAccessibility == Accessibility.Public &&
                    m.MethodKind == MethodKind.Ordinary &&
                    m.GetAttributes().Any(attr =>
                        attr.AttributeClass is not null &&
                        InheritsFromAnyAttribute(attr.AttributeClass, facadeMethodAttributeTypes)))
                .ToArray();

            // If type has class-level or method-level attributes, add it
            if (classLevelAttributes.Length > 0 || methodsWithAttributes.Length > 0)
            {
                result.Add(new OpenGenericTypeInfo(
                    type,
                    classLevelAttributes.ToImmutableArray(),
                    methodsWithAttributes.ToImmutableArray()));
            }
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Discovers all closed generic instantiations of the specified open generic types.
    /// </summary>
    private static ImmutableArray<ClosedGenericInstantiation> DiscoverClosedGenericInstantiations(
        Compilation compilation,
        ImmutableArray<OpenGenericTypeInfo> openGenericTypes,
        CancellationToken ct)
    {
        var result = new Dictionary<string, ClosedGenericInstantiation>();

        // Walk all types in the compilation looking for closed generic usages
        foreach (var type in GetAllTypes(compilation.Assembly.GlobalNamespace, ct))
        {
            ct.ThrowIfCancellationRequested();

            // Check if this type implements/derives from any open generic
            foreach (var openGenericInfo in openGenericTypes)
            {
                // Skip the open generic type itself
                if (SymbolEqualityComparer.Default.Equals(type, openGenericInfo.OpenGenericType))
                    continue;

                var closedVersions = FindClosedGenericUsages(type, openGenericInfo.OpenGenericType);
                foreach (var closedType in closedVersions)
                {
                    var key = closedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (!result.ContainsKey(key))
                    {
                        result[key] = new ClosedGenericInstantiation(
                            closedType,
                            openGenericInfo.OpenGenericType,
                            openGenericInfo.ClassLevelAttributes,
                            openGenericInfo.MethodsWithAttributes);
                    }
                }
            }
        }

        return result.Values.ToImmutableArray();
    }

    /// <summary>
    /// Finds all closed generic versions of an open generic type used by a given type.
    /// </summary>
    private static IEnumerable<INamedTypeSymbol> FindClosedGenericUsages(
        INamedTypeSymbol type,
        INamedTypeSymbol openGenericType)
    {
        var result = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        // Check if the type itself is a closed generic of the open generic
        if (type.IsGenericType && !type.IsUnboundGenericType)
        {
            var originalDefinition = type.OriginalDefinition;
            if (SymbolEqualityComparer.Default.Equals(originalDefinition, openGenericType))
            {
                // The type itself is a closed generic (e.g., StringHandler implements IHandler<string>)
                // We don't add the type itself, but we check its interfaces and base types
            }
        }

        // Check base type
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType && !baseType.IsUnboundGenericType)
            {
                var originalDefinition = baseType.OriginalDefinition;
                if (SymbolEqualityComparer.Default.Equals(originalDefinition, openGenericType))
                {
                    result.Add(baseType);
                }
            }
            baseType = baseType.BaseType;
        }

        // Check interfaces
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.IsGenericType && !iface.IsUnboundGenericType)
            {
                var originalDefinition = iface.OriginalDefinition;
                if (SymbolEqualityComparer.Default.Equals(originalDefinition, openGenericType))
                {
                    result.Add(iface);
                }
            }
        }

        // Check fields and properties
        foreach (var member in type.GetMembers())
        {
            ITypeSymbol? memberType = member switch
            {
                IFieldSymbol field => field.Type,
                IPropertySymbol property => property.Type,
                _ => null
            };

            if (memberType is INamedTypeSymbol namedType &&
                namedType.IsGenericType &&
                !namedType.IsUnboundGenericType)
            {
                var originalDefinition = namedType.OriginalDefinition;
                if (SymbolEqualityComparer.Default.Equals(originalDefinition, openGenericType))
                {
                    result.Add(namedType);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Expands closed generic instantiations into CandidateMethodInfo entries.
    /// </summary>
    private static ImmutableArray<CandidateMethodInfo> ExpandClosedGenericsToMethods(
        Compilation compilation,
        ImmutableArray<ClosedGenericInstantiation> closedInstantiations,
        ImmutableArray<INamedTypeSymbol> facadeMethodAttributeTypes,
        CancellationToken ct)
    {
        var result = ImmutableArray.CreateBuilder<CandidateMethodInfo>();
        var seenMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        foreach (var instantiation in closedInstantiations)
        {
            ct.ThrowIfCancellationRequested();

            var publicMethods = instantiation.ClosedGenericType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m =>
                    m.DeclaredAccessibility == Accessibility.Public &&
                    m.MethodKind == MethodKind.Ordinary);

            foreach (var method in publicMethods)
            {
                ct.ThrowIfCancellationRequested();

                // Skip if we've already processed this exact method symbol
                if (!seenMethods.Add(method))
                    continue;

                // Check if this method has explicit attributes
                var methodAttributes = method.GetAttributes()
                    .Where(attr => attr.AttributeClass is not null &&
                                   InheritsFromAnyAttribute(attr.AttributeClass, facadeMethodAttributeTypes))
                    .ToArray();

                // If method has explicit attributes, use those
                if (methodAttributes.Length > 0)
                {
                    foreach (var attr in methodAttributes)
                    {
                        var returnTypeKind = compilation.ResolveReturnTypeKind(method);
                        var documentationXml = method.GetDocumentationCommentXml(cancellationToken: ct);
                        result.Add(new CandidateMethodInfo(
                            method,
                            attr,
                            returnTypeKind,
                            documentationXml,
                            instantiation.OpenGenericType,
                            instantiation.ClosedGenericType.TypeArguments));
                    }
                }
                // Otherwise, check if we should apply class-level attributes
                else if (!instantiation.ClassLevelAttributes.IsEmpty)
                {
                    // Check if original open generic method had explicit attributes
                    var originalMethod = instantiation.MethodsWithAttributes
                        .FirstOrDefault(m => m.Name == method.Name &&
                                            ParametersMatch(m, method));

                    // If original method had explicit attributes, skip class-level application
                    if (originalMethod != null)
                        continue;

                    // Apply class-level attributes
                    foreach (var attr in instantiation.ClassLevelAttributes)
                    {
                        var returnTypeKind = compilation.ResolveReturnTypeKind(method);
                        var documentationXml = method.GetDocumentationCommentXml(cancellationToken: ct);
                        result.Add(new CandidateMethodInfo(
                            method,
                            attr,
                            returnTypeKind,
                            documentationXml,
                            instantiation.OpenGenericType,
                            instantiation.ClosedGenericType.TypeArguments));
                    }
                }
            }
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Checks if two methods have matching parameters (ignoring type parameter substitution).
    /// </summary>
    private static bool ParametersMatch(IMethodSymbol method1, IMethodSymbol method2)
    {
        if (method1.Parameters.Length != method2.Parameters.Length)
            return false;

        for (int i = 0; i < method1.Parameters.Length; i++)
        {
            // Simple check - just compare parameter count and names
            // Type substitution is expected, so we don't compare types
            if (method1.Parameters[i].Name != method2.Parameters[i].Name)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Recursively enumerates all types in a namespace and its nested namespaces.
    /// </summary>
    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol namespaceSymbol, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            yield return type;

            // Include nested types
            foreach (var nestedType in GetNestedTypes(type, ct))
            {
                yield return nestedType;
            }
        }

        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypes(nestedNamespace, ct))
            {
                yield return type;
            }
        }
    }

    /// <summary>
    /// Recursively enumerates all nested types within a type.
    /// </summary>
    private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var nestedType in type.GetTypeMembers())
        {
            yield return nestedType;

            foreach (var deeplyNested in GetNestedTypes(nestedType, ct))
            {
                yield return deeplyNested;
            }
        }
    }

    /// <summary>
    /// Checks if an attribute class inherits from any of the specified attribute types.
    /// </summary>
    private static bool InheritsFromAnyAttribute(
        INamedTypeSymbol attributeClass,
        ImmutableArray<INamedTypeSymbol> targetAttributeTypes)
    {
        var current = attributeClass;
        while (current != null)
        {
            if (targetAttributeTypes.Any(t => SymbolEqualityComparer.Default.Equals(current, t)))
                return true;

            current = current.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Represents an open generic type with facade method attributes.
    /// </summary>
    private readonly record struct OpenGenericTypeInfo(
        INamedTypeSymbol OpenGenericType,
        ImmutableArray<AttributeData> ClassLevelAttributes,
        ImmutableArray<IMethodSymbol> MethodsWithAttributes);

    /// <summary>
    /// Represents a closed generic instantiation of an open generic type.
    /// </summary>
    private readonly record struct ClosedGenericInstantiation(
        INamedTypeSymbol ClosedGenericType,
        INamedTypeSymbol OpenGenericType,
        ImmutableArray<AttributeData> ClassLevelAttributes,
        ImmutableArray<IMethodSymbol> MethodsWithAttributes);

    /// <summary>
    /// Reports diagnostics for open generic types that have no closed generic instantiations.
    /// </summary>
    private static void ReportMissingInstantiations(
        SourceProductionContext context,
        ImmutableArray<OpenGenericTypeInfo> openGenericTypes,
        ImmutableArray<ClosedGenericInstantiation> closedInstantiations)
    {
        // Build set of open generic types that have closed instantiations
        var instantiatedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var closedInstantiation in closedInstantiations)
        {
            instantiatedTypes.Add(closedInstantiation.OpenGenericType);
        }

        // Report warning for open generics without instantiations
        foreach (var openGenericInfo in openGenericTypes)
        {
            if (!instantiatedTypes.Contains(openGenericInfo.OpenGenericType))
            {
                var diagnostic = Diagnostic.Create(
                    Diagnostics.NoClosedGenericInstantiationsFound,
                    openGenericInfo.OpenGenericType.Locations.FirstOrDefault(),
                    openGenericInfo.OpenGenericType.ToDisplayString());

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
