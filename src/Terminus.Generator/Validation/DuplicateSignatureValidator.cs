using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Terminus.Generator.Builders.Naming;

namespace Terminus.Generator.Validation;

/// <summary>
/// Validates that there are no duplicate method signatures within the same facade.
/// </summary>
internal class DuplicateSignatureValidator : IMethodValidator
{
    private readonly Dictionary<MethodSignature, List<IMethodSymbol>> _signatures = new(MethodSignatureEqualityComparer.Instance);
    private bool _hasErrors;

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
        var diagnostics = _signatures
            .Select(entry => entry.Value)
            .Where(symbols => symbols.Count > 1 && symbols.Distinct(SymbolEqualityComparer.Default).Count() > 1)
            .SelectMany(
                symbols => symbols,
                (_, symbol) =>
                    Diagnostic.Create(
                        Diagnostics.DuplicateFacadeMethodSignature,
                        symbol.Locations.FirstOrDefault(),
                        symbol.Name));
        
        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
            _hasErrors = true;
        }

        return _hasErrors;
    }

    private static MethodSignature GetMethodSignature(CandidateMethodInfo methodInfo, FacadeInterfaceInfo facadeInfo)
    {
        var methodName = MethodNamingStrategy.GetMethodName(facadeInfo, methodInfo);
        return new MethodSignature(
            methodName,
            [..methodInfo.MethodSymbol.Parameters.Select(p => p.Type)]);
    }

    private readonly struct MethodSignature(string name, ImmutableArray<ITypeSymbol> parameterTypes)
    {
        public string Name { get; } = name;
        public ImmutableArray<ITypeSymbol> ParameterTypes { get; } = parameterTypes;
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

            return !x.ParameterTypes
                .Where((t, i) => 
                    !SymbolEqualityComparer.Default.Equals(t, y.ParameterTypes[i]))
                .Any();
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
                return hash;
            }
        }
    }
}
