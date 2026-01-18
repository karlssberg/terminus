using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Terminus.Generator.Builders.Naming;

namespace Terminus.Generator.Validation;

/// <summary>
/// Validates that method names (after naming strategy is applied) don't conflict with property names.
/// </summary>
internal sealed class MethodPropertyConflictValidator : IMethodValidator
{
    private readonly List<(string MethodName, IMethodSymbol MethodSymbol)> _methodNames = [];
    private ImmutableArray<CandidatePropertyInfo> _properties = ImmutableArray<CandidatePropertyInfo>.Empty;
    private FacadeInterfaceInfo? _facadeInfo;

    /// <summary>
    /// Sets the properties to validate against.
    /// </summary>
    public void SetProperties(ImmutableArray<CandidatePropertyInfo> properties)
    {
        _properties = properties;
    }

    /// <inheritdoc />
    public void Add(CandidateMethodInfo methodInfo, FacadeInterfaceInfo facadeInfo)
    {
        _facadeInfo = facadeInfo;
        var methodName = MethodNamingStrategy.GetMethodName(facadeInfo, methodInfo);
        _methodNames.Add((methodName, methodInfo.MethodSymbol));
    }

    /// <inheritdoc />
    public bool Validate(SourceProductionContext context)
    {
        if (_properties.IsEmpty || _methodNames.Count == 0)
            return false;

        var hasErrors = false;

        // Build a set of property names for fast lookup
        var propertyNames = new HashSet<string>(
            _properties.Select(p => p.PropertySymbol.Name),
            StringComparer.Ordinal);

        // Check each method name against property names
        foreach (var (methodName, methodSymbol) in _methodNames)
        {
            if (!propertyNames.Contains(methodName))
                continue;

            // Find the conflicting property for the message
            var conflictingProperty = _properties
                .First(p => string.Equals(p.PropertySymbol.Name, methodName, StringComparison.Ordinal));

            var location = methodSymbol.Locations.FirstOrDefault();
            var diagnostic = Diagnostic.Create(
                Diagnostics.MethodPropertyNameConflict,
                location,
                methodName,
                conflictingProperty.PropertySymbol.Name,
                _facadeInfo?.InterfaceSymbol.Name ?? "Unknown");
            context.ReportDiagnostic(diagnostic);
            hasErrors = true;
        }

        return hasErrors;
    }
}
