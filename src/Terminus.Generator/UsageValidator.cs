using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal static class UsageValidator
{
    internal static bool Validate(SourceProductionContext context, ImmutableArray<EntryPointMethodInfo> entryPointMethodInfos,
        AggregatorInterfaceInfo facade)
    {
        var hasErrors = false;
        
        // TM0003: No ref/out parameters allowed
        foreach (var entryPoint in entryPointMethodInfos)
        {
            var refOrOutParameters = entryPoint.MethodSymbol
                .Parameters.Where(p => p.RefKind is RefKind.Ref or RefKind.Out);
            
            foreach (var parameter in refOrOutParameters)
            {
                var diagnostic = Diagnostic.Create(
                    Diagnostics.RefOrOutParameter,
                    parameter.Locations.FirstOrDefault() ?? entryPoint.MethodSymbol.Locations.FirstOrDefault(),
                    entryPoint.MethodSymbol.Name,
                    parameter.Name);
                context.ReportDiagnostic(diagnostic);
                hasErrors = true;
            }
        }

        // TM0002: Generic methods not allowed
        var genericEntryPointMethods = entryPointMethodInfos
            .Where(ep => ep.MethodSymbol.IsGenericMethod );
        
        foreach (var entryPoint in genericEntryPointMethods)
        {
            var diagnostic = Diagnostic.Create(
                Diagnostics.GenericEntryPointMethod,
                entryPoint.MethodSymbol.Locations.FirstOrDefault(),
                entryPoint.MethodSymbol.Name);
            context.ReportDiagnostic(diagnostic);
            hasErrors = true;
        }

        // TM0001: Detect duplicate signatures
        var duplicates = entryPointMethodInfos
            .GroupBy(ep => GetMethodSignature(ep.MethodSymbol), MethodSignatureEqualityComparer.Instance)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g);

        foreach (var duplicate in duplicates)
        {
            var diagnostic = Diagnostic.Create(
                Diagnostics.DuplicateEntryPointSignature,
                duplicate.MethodSymbol.Locations.FirstOrDefault(),
                duplicate.MethodSymbol.Name);
            context.ReportDiagnostic(diagnostic);
            hasErrors = true;
        }

        return hasErrors;
    }
    
    private static MethodSignature GetMethodSignature(IMethodSymbol method)
    {
        return new MethodSignature(
            method.Name,
            [..method.Parameters.Select(p => p.Type)]);
    }

    private readonly struct MethodSignature
    {
        public MethodSignature(string name, ImmutableArray<ITypeSymbol> parameterTypes)
        {
            Name = name;
            ParameterTypes = parameterTypes;
        }

        public string Name { get; }
        public ImmutableArray<ITypeSymbol> ParameterTypes { get; }
    }

    private class MethodSignatureEqualityComparer : IEqualityComparer<MethodSignature>
    {
        public static readonly MethodSignatureEqualityComparer Instance = new MethodSignatureEqualityComparer();

        public bool Equals(MethodSignature x, MethodSignature y)
        {
            if (x.Name != y.Name)
                return false;

            if (x.ParameterTypes.Length != y.ParameterTypes.Length)
                return false;

            for (int i = 0; i < x.ParameterTypes.Length; i++)
            {
                if (!SymbolEqualityComparer.Default.Equals(x.ParameterTypes[i], y.ParameterTypes[i]))
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
                return hash;
            }
        }
    }
}