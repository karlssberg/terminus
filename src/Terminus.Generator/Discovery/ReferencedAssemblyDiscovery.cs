using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Terminus.Generator.Discovery;

/// <summary>
/// Discovers facade methods from referenced assemblies.
/// Used when <c>IncludeReferencedAssemblies = true</c> is set on a facade interface.
/// </summary>
internal static class ReferencedAssemblyDiscovery
{
    /// <summary>
    /// Discovers methods from referenced assemblies based on the specified discovery mode.
    /// </summary>
    /// <param name="compilation">The current compilation.</param>
    /// <param name="facadeMethodAttributeTypes">The attribute types that identify facade methods.</param>
    /// <param name="discoveryMode">The mode determining which assemblies to scan.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of candidate method info from referenced assemblies.</returns>
    public static ImmutableArray<CandidateMethodInfo> DiscoverMethodsFromReferencedAssemblies(
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> facadeMethodAttributeTypes,
        MethodDiscoveryMode discoveryMode,
        CancellationToken ct)
    {
        if (discoveryMode == MethodDiscoveryMode.None)
            return ImmutableArray<CandidateMethodInfo>.Empty;

        var assembliesToScan = GetAssembliesToScan(compilation, discoveryMode);
        var result = ImmutableArray.CreateBuilder<CandidateMethodInfo>();

        foreach (var assemblySymbol in assembliesToScan)
        {
            ct.ThrowIfCancellationRequested();

            // Skip assemblies that shouldn't be scanned
            if (!ShouldScanAssembly(assemblySymbol))
                continue;

            // Discover methods from this assembly
            var methods = DiscoverMethodsFromAssembly(
                compilation,
                assemblySymbol,
                facadeMethodAttributeTypes,
                ct);

            result.AddRange(methods);
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Gets the assemblies to scan based on the discovery mode.
    /// </summary>
    private static IEnumerable<IAssemblySymbol> GetAssembliesToScan(
        Compilation compilation,
        MethodDiscoveryMode discoveryMode)
    {
        return discoveryMode switch
        {
            MethodDiscoveryMode.ReferencedAssemblies => GetDirectlyReferencedAssemblies(compilation),
            MethodDiscoveryMode.TransitiveAssemblies => GetAllReferencedAssemblies(compilation),
            _ => Enumerable.Empty<IAssemblySymbol>()
        };
    }

    /// <summary>
    /// Gets only directly referenced assemblies (not transitive dependencies).
    /// Uses the compilation's main assembly module to determine direct references.
    /// </summary>
    private static IEnumerable<IAssemblySymbol> GetDirectlyReferencedAssemblies(Compilation compilation)
    {
        // Get assemblies directly referenced by the compilation's main assembly
        foreach (var module in compilation.Assembly.Modules)
        {
            foreach (var referencedAssembly in module.ReferencedAssemblySymbols)
            {
                yield return referencedAssembly;
            }
        }
    }

    /// <summary>
    /// Gets all referenced assemblies including transitive dependencies.
    /// Uses compilation.References which includes all available assemblies.
    /// </summary>
    private static IEnumerable<IAssemblySymbol> GetAllReferencedAssemblies(Compilation compilation)
    {
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assemblySymbol)
            {
                yield return assemblySymbol;
            }
        }
    }

    /// <summary>
    /// Determines if an assembly should be scanned for facade methods.
    /// Skips well-known system assemblies for performance.
    /// </summary>
    private static bool ShouldScanAssembly(IAssemblySymbol assembly)
    {
        var name = assembly.Name;

        // Skip well-known system assemblies
        if (name.StartsWith("System.", StringComparison.Ordinal) ||
            name.StartsWith("Microsoft.", StringComparison.Ordinal) ||
            name == "mscorlib" ||
            name == "netstandard" ||
            name == "Terminus") // Skip Terminus itself
            return false;

        return true;
    }

    /// <summary>
    /// Discovers methods from a single assembly that have the specified facade method attributes.
    /// </summary>
    private static ImmutableArray<CandidateMethodInfo> DiscoverMethodsFromAssembly(
        Compilation compilation,
        IAssemblySymbol assembly,
        ImmutableArray<INamedTypeSymbol> facadeTargetAttributeTypes,
        CancellationToken ct)
    {
        var result = ImmutableArray.CreateBuilder<CandidateMethodInfo>();

        // Walk all types in the assembly
        foreach (var type in GetAllTypes(assembly.GlobalNamespace, ct))
        {
            ct.ThrowIfCancellationRequested();

            // Check for class-level attributes
            var matchingTypeAttributes = type.GetAttributes()
                .Where(attr => attr.AttributeClass is not null &&
                               InheritsFromAnyAttribute(attr.AttributeClass, facadeTargetAttributeTypes))
                .ToArray();

            // Get public methods
            var publicMethods = type.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m =>
                    m.DeclaredAccessibility == Accessibility.Public &&
                    m.MethodKind == MethodKind.Ordinary);

            foreach (var method in publicMethods)
            {
                ct.ThrowIfCancellationRequested();

                // Check method-level attributes
                var matchingMethodAttributes = method.GetAttributes()
                    .Where(attr => attr.AttributeClass is not null &&
                                   InheritsFromAnyAttribute(attr.AttributeClass, facadeTargetAttributeTypes))
                    .ToArray();

                // Add CandidateMethodInfo for each matching method attribute
                foreach (var attr in matchingMethodAttributes)
                {
                    var returnTypeKind = compilation.ResolveReturnTypeKind(method);
                    var documentationXml = method.GetDocumentationCommentXml(cancellationToken: ct);
                    result.Add(new CandidateMethodInfo(method, attr, returnTypeKind, documentationXml));
                }

                // If method has its own matching attributes, skip type-level ones
                if (matchingMethodAttributes.Length > 0)
                    continue;
                
                // Add CandidateMethodInfo for each type-level attribute (if method doesn't have its own)
                foreach (var attr in matchingTypeAttributes)
                {
                    var returnTypeKind = compilation.ResolveReturnTypeKind(method);
                    var documentationXml = method.GetDocumentationCommentXml(cancellationToken: ct);
                    result.Add(new CandidateMethodInfo(method, attr, returnTypeKind, documentationXml));
                }
            }
        }

        return result.ToImmutable();
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
    /// Uses fully qualified name comparison to work across assembly boundaries.
    /// </summary>
    private static bool InheritsFromAnyAttribute(
        INamedTypeSymbol attributeClass,
        ImmutableArray<INamedTypeSymbol> targetAttributeTypes)
    {
        // Build a set of fully qualified names for the target attribute types
        var targetNames = new HashSet<string>(
            targetAttributeTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

        return InheritsFromAnyAttributeByName(attributeClass, targetNames);
    }

    /// <summary>
    /// Checks if an attribute class inherits from any of the specified attribute type names.
    /// </summary>
    private static bool InheritsFromAnyAttributeByName(
        INamedTypeSymbol attributeClass,
        HashSet<string> targetAttributeNames)
    {
        var current = attributeClass;
        while (current != null)
        {
            var currentName = current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (targetAttributeNames.Contains(currentName))
                return true;

            current = current.BaseType;
        }

        return false;
    }
}
