using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Terminus.Generator.Builders.Naming;

namespace Terminus.Generator.Validation;

/// <summary>
/// Tracks duplicate method signatures for aggregation.
/// Duplicate signatures are now a feature (like MediatR notifications) rather than an error.
/// </summary>
internal class DuplicateSignatureValidator : IMethodValidator
{
    private readonly Dictionary<MethodSignature, List<IMethodSymbol>> _signatures = new(MethodSignatureEqualityComparer.Instance);

    /// <inheritdoc />
    public void Add(CandidateMethodInfo methodInfo, FacadeInterfaceInfo facadeInfo)
    {
        var signature = GetMethodSignature(methodInfo, facadeInfo);
        if (_signatures.TryGetValue(signature, out var symbols))
        {
            symbols.Add(methodInfo.MethodSymbol);
            return;
        }

        _signatures[signature] = [methodInfo.MethodSymbol];
    }

    /// <inheritdoc />
    public bool Validate(SourceProductionContext context)
    {
        // Duplicate signatures are allowed for aggregation (similar to MediatR notifications)
        // No validation errors to report
        return false;
    }

    private static MethodSignature GetMethodSignature(CandidateMethodInfo methodInfo, FacadeInterfaceInfo facadeInfo)
    {
        var methodName = MethodNamingStrategy.GetMethodName(facadeInfo, methodInfo);
        var methodSymbol = methodInfo.MethodSymbol;

        // Include return type for overloading, though C# generally doesn't allow it, 
        // the facade might map different return types to the same name.
        // But more importantly, include generic constraints.
        
        var constraints = methodSymbol.TypeParameters
            .Select(tp => tp.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .ToImmutableArray();

        return new MethodSignature(
            methodName,
            [..methodSymbol.Parameters.Select(p => p.Type)],
            constraints);
    }

    private readonly struct MethodSignature(
        string name, 
        ImmutableArray<ITypeSymbol> parameterTypes,
        ImmutableArray<string> genericConstraints)
    {
        public string Name { get; } = name;
        public ImmutableArray<ITypeSymbol> ParameterTypes { get; } = parameterTypes;
        public ImmutableArray<string> GenericConstraints { get; } = genericConstraints;
    }

    private class MethodSignatureEqualityComparer : IEqualityComparer<MethodSignature>
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

            for (var i = 0; i < x.ParameterTypes.Length; i++)
            {
                if (!SymbolEqualityComparer.Default.Equals(x.ParameterTypes[i], y.ParameterTypes[i]))
                    return false;
            }

            for (var i = 0; i < x.GenericConstraints.Length; i++)
            {
                if (x.GenericConstraints[i] != y.GenericConstraints[i])
                    return false;
            }

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
                    hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(paramType);
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
