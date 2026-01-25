using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Terminus.Generator.Builders.Naming;

namespace Terminus.Generator.Grouping;

/// <summary>
/// Groups methods by their signature for aggregation.
/// </summary>
internal static class MethodSignatureGrouper
{
    /// <summary>
    /// Groups methods by signature. Methods with the same signature will be aggregated
    /// only if aggregation is enabled for their return type via AggregationMode.
    /// </summary>
    public static ImmutableArray<AggregatedMethodGroup> GroupBySignature(
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<CandidateMethodInfo> methods,
        Compilation compilation)
    {
        var includeMetadata = facadeInfo.Features.IncludeAttributeMetadata;

        var groups = methods
            .GroupBy(
                method => GetGroupingKey(facadeInfo, method),
                GroupingKeyEqualityComparer.Instance)
            .Select(group =>
            {
                var methodsList = group.ToImmutableArray();

                // Compute common attribute type when metadata is enabled (works for single or multiple methods)
                INamedTypeSymbol? commonAttributeType = null;
                if (includeMetadata)
                {
                    commonAttributeType = FindCommonBaseAttributeType(methodsList, compilation);
                }

                return new AggregatedMethodGroup(methodsList, commonAttributeType);
            })
            .ToImmutableArray();

        return groups;
    }

    /// <summary>
    /// Finds the lowest common ancestor (LCA) attribute type among all methods in the group.
    /// </summary>
    private static INamedTypeSymbol? FindCommonBaseAttributeType(
        ImmutableArray<CandidateMethodInfo> methods,
        Compilation compilation)
    {
        var attributeTypes = methods
            .Select(m => m.AttributeData.AttributeClass)
            .Where(a => a != null)
            .Cast<INamedTypeSymbol>()
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<INamedTypeSymbol>()
            .ToList();

        if (attributeTypes.Count == 0)
            return null;

        if (attributeTypes.Count == 1)
            return attributeTypes[0];

        // Get all base types for the first attribute
        var commonBases = new HashSet<ISymbol>(
            GetBaseTypeHierarchy(attributeTypes[0]),
            SymbolEqualityComparer.Default);

        // Intersect with base types of remaining attributes
        foreach (var type in attributeTypes.Skip(1))
        {
            var bases = new HashSet<ISymbol>(
                GetBaseTypeHierarchy(type),
                SymbolEqualityComparer.Default);
            commonBases.IntersectWith(bases);
        }

        // Return the most derived common base (closest to leaves)
        // by ordering by depth from System.Attribute
        return commonBases
            .Cast<INamedTypeSymbol>()
            .OrderByDescending(GetDepthFromAttribute)
            .FirstOrDefault();
    }

    private static IEnumerable<INamedTypeSymbol> GetBaseTypeHierarchy(INamedTypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;

            // Stop at System.Attribute - don't include Object in the hierarchy
            if (current.Name == "Attribute" &&
                current.ContainingNamespace?.ToDisplayString() == "System")
            {
                yield break;
            }

            current = current.BaseType;
        }
    }

    private static int GetDepthFromAttribute(INamedTypeSymbol type)
    {
        var depth = 0;
        var current = type;
        while (current != null && current.Name != "Attribute")
        {
            depth++;
            current = current.BaseType;
        }
        return depth;
    }

    private static GroupingKey GetGroupingKey(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo)
    {
        var methodSignature = GetMethodSignature(facadeInfo, methodInfo);
        var shouldAggregate = ShouldAggregate(facadeInfo, methodInfo);

        // If aggregation is disabled for this return type, include the method symbol in the key
        // to ensure each method gets its own group
        return new GroupingKey(methodSignature, shouldAggregate ? null : methodInfo.MethodSymbol);
    }

    private static bool ShouldAggregate(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo)
    {
        var mode = facadeInfo.Features.AggregationMode;

        // FacadeAggregationMode enum values (from Terminus assembly)
        const int None = 0;
        const int Commands = 1 << 0;      // 1
        const int Queries = 1 << 1;       // 2
        const int AsyncCommands = 1 << 2; // 4
        const int AsyncQueries = 1 << 3;  // 8
        const int AsyncStreams = 1 << 4;  // 16

        // When AggregationMode is None (default), no aggregation - single handler only
        if (mode == None)
            return false;

        // When AggregationMode flags are set, aggregate methods with matching return types
        return methodInfo.ReturnTypeKind switch
        {
            ReturnTypeKind.Void => (mode & Commands) != 0,
            ReturnTypeKind.Result => (mode & Queries) != 0,
            ReturnTypeKind.Task or ReturnTypeKind.ValueTask => (mode & AsyncCommands) != 0,
            ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult => (mode & AsyncQueries) != 0,
            ReturnTypeKind.AsyncEnumerable => (mode & AsyncStreams) != 0,
            _ => false
        };
    }

    private static MethodSignature GetMethodSignature(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo)
    {
        var methodName = MethodNamingStrategy.GetMethodName(facadeInfo, methodInfo);
        var methodSymbol = methodInfo.MethodSymbol;

        var constraints = methodSymbol.TypeParameters
            .Select(tp => tp.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .ToImmutableArray();

        return new MethodSignature(
            methodName,
            [..methodSymbol.Parameters.Select(p => p.Type)],
            constraints);
    }

    private readonly record struct MethodSignature(
        string Name,
        ImmutableArray<ITypeSymbol> ParameterTypes,
        ImmutableArray<string> GenericConstraints);

    private readonly record struct GroupingKey(
        MethodSignature Signature,
        IMethodSymbol? MethodSymbol);

    private sealed class GroupingKeyEqualityComparer : IEqualityComparer<GroupingKey>
    {
        public static readonly GroupingKeyEqualityComparer Instance = new();

        public bool Equals(GroupingKey x, GroupingKey y)
        {
            // If method symbols are different (and both not null), they're different groups
            if (x.MethodSymbol != null && y.MethodSymbol != null)
            {
                if (!SymbolEqualityComparer.Default.Equals(x.MethodSymbol, y.MethodSymbol))
                    return false;
            }

            // Otherwise, compare signatures
            return MethodSignatureEqualityComparer.Instance.Equals(x.Signature, y.Signature);
        }

        public int GetHashCode(GroupingKey obj)
        {
            unchecked
            {
                var hash = MethodSignatureEqualityComparer.Instance.GetHashCode(obj.Signature);
                if (obj.MethodSymbol != null)
                {
                    hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(obj.MethodSymbol);
                }
                return hash;
            }
        }
    }

    private sealed class MethodSignatureEqualityComparer : IEqualityComparer<MethodSignature>
    {
        public static readonly MethodSignatureEqualityComparer Instance = new();

        public bool Equals(MethodSignature x, MethodSignature y)
        {
            if (x.Name != y.Name)
                return false;

            if (x.ParameterTypes.Length != y.ParameterTypes.Length)
                return false;

            if (x.GenericConstraints.Length != y.GenericConstraints.Length)
                return false;

            if (!x.ParameterTypes.AsEnumerable().SequenceEqual(y.ParameterTypes, SymbolEqualityComparer.Default))
                return false;

            if (!x.GenericConstraints.AsEnumerable().SequenceEqual(y.GenericConstraints))
                return false;

            return true;
        }

        public int GetHashCode(MethodSignature obj)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + (obj.Name?.GetHashCode() ?? 0);
                hash = hash * 31 + obj.ParameterTypes.Length;
                hash = obj.ParameterTypes.Aggregate(hash, (current, paramType) =>
                    current * 31 + SymbolEqualityComparer.Default.GetHashCode(paramType));

                hash = hash * 31 + obj.GenericConstraints.Length;
                hash = obj.GenericConstraints.Aggregate(hash, (current, constraint) =>
                    current * 31 + (constraint?.GetHashCode() ?? 0));

                return hash;
            }
        }
    }
}
