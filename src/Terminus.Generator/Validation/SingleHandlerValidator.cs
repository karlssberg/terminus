using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Terminus.Generator.Builders.Naming;

namespace Terminus.Generator.Validation;

/// <summary>
/// Validates that when AggregationMode is None (or doesn't include the appropriate flag for a return type),
/// each method signature has exactly one handler.
/// </summary>
internal class SingleHandlerValidator : IMethodValidator
{
    // FacadeAggregationMode enum values (from Terminus assembly)
    private const int None = 0;
    private const int Commands = 1 << 0;      // 1
    private const int Queries = 1 << 1;       // 2
    private const int AsyncCommands = 1 << 2; // 4
    private const int AsyncQueries = 1 << 3;  // 8
    private const int AsyncStreams = 1 << 4;  // 16

    private readonly Dictionary<MethodSignature, List<(IMethodSymbol Symbol, CandidateMethodInfo Info)>> _signatures = new(MethodSignatureEqualityComparer.Instance);
    private FacadeInterfaceInfo? _facadeInfo;

    /// <inheritdoc />
    public void Add(CandidateMethodInfo methodInfo, FacadeInterfaceInfo facadeInfo)
    {
        _facadeInfo = facadeInfo;

        var signature = GetMethodSignature(methodInfo, facadeInfo);
        if (_signatures.TryGetValue(signature, out var methods))
        {
            methods.Add((methodInfo.MethodSymbol, methodInfo));
            return;
        }

        _signatures[signature] = [(methodInfo.MethodSymbol, methodInfo)];
    }

    /// <inheritdoc />
    public bool Validate(SourceProductionContext context)
    {
        if (_facadeInfo is null)
            return false;

        var hasErrors = false;

        foreach (var kvp in _signatures)
        {
            var signature = kvp.Key;
            var methods = kvp.Value;

            // Skip if only one handler
            if (methods.Count <= 1)
                continue;

            // Check if aggregation is enabled for this return type
            var firstMethod = methods[0].Info;
            if (IsAggregationEnabledFor(_facadeInfo.Value.Features.AggregationMode, firstMethod.ReturnTypeKind))
                continue;

            // Multiple handlers with same signature and aggregation not enabled - report error for all but the first
            foreach (var method in methods.Skip(1))
            {
                var diagnostic = Diagnostic.Create(
                    Diagnostics.MultipleHandlersWithoutAggregation,
                    method.Symbol.Locations.FirstOrDefault(),
                    signature.ToDisplayString());
                context.ReportDiagnostic(diagnostic);
                hasErrors = true;
            }
        }

        return hasErrors;
    }

    /// <summary>
    /// Checks if aggregation is enabled for the given return type based on the aggregation mode.
    /// </summary>
    private static bool IsAggregationEnabledFor(int aggregationMode, ReturnTypeKind returnTypeKind)
    {
        // When AggregationMode is None (default), no aggregation is allowed
        if (aggregationMode == None)
            return false;

        // Check if the specific flag is set for this return type
        return returnTypeKind switch
        {
            ReturnTypeKind.Void => (aggregationMode & Commands) != 0,
            ReturnTypeKind.Result => (aggregationMode & Queries) != 0,
            ReturnTypeKind.Task or ReturnTypeKind.ValueTask => (aggregationMode & AsyncCommands) != 0,
            ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult => (aggregationMode & AsyncQueries) != 0,
            ReturnTypeKind.AsyncEnumerable => (aggregationMode & AsyncStreams) != 0,
            _ => false
        };
    }

    private static MethodSignature GetMethodSignature(CandidateMethodInfo methodInfo, FacadeInterfaceInfo facadeInfo)
    {
        var methodName = MethodNamingStrategy.GetMethodName(facadeInfo, methodInfo);
        var methodSymbol = methodInfo.MethodSymbol;

        var constraints = methodSymbol.TypeParameters
            .Select(tp => tp.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .ToImmutableArray();

        var parameterTypes = methodSymbol.Parameters
            .Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .ToImmutableArray();

        return new MethodSignature(
            methodName,
            parameterTypes,
            constraints);
    }

    private readonly struct MethodSignature(
        string name,
        ImmutableArray<string> parameterTypes,
        ImmutableArray<string> genericConstraints)
    {
        public string Name { get; } = name;
        public ImmutableArray<string> ParameterTypes { get; } = parameterTypes;
        public ImmutableArray<string> GenericConstraints { get; } = genericConstraints;

        public string ToDisplayString()
        {
            var parameters = string.Join(", ", ParameterTypes);
            return $"{Name}({parameters})";
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

            if (!x.ParameterTypes.SequenceEqual(y.ParameterTypes))
                return false;

            if (!x.GenericConstraints.SequenceEqual(y.GenericConstraints))
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
                foreach (var paramType in obj.ParameterTypes)
                {
                    hash = hash * 31 + (paramType?.GetHashCode() ?? 0);
                }

                hash = hash * 31 + obj.GenericConstraints.Length;
                foreach (var constraint in obj.GenericConstraints)
                {
                    hash = hash * 31 + (constraint?.GetHashCode() ?? 0);
                }

                return hash;
            }
        }
    }
}
