using Microsoft.CodeAnalysis;
using Terminus.Generator.Builders.Naming;

namespace Terminus.Generator.Validation;

/// <summary>
/// Validates that method and parameter names do not conflict with internal field names used in the generated implementation.
/// </summary>
internal class ConflictingNameValidator : IMethodValidator
{
    private static readonly HashSet<string> ReservedNames = 
    [
        "_serviceProvider",
        "_syncScope",
        "_asyncScope",
        "_syncDisposed",
        "_asyncDisposed"
    ];

    private readonly List<CandidateMethodInfo> _methods = [];
    private FacadeInterfaceInfo? _facadeInfo;

    /// <inheritdoc />
    public void Add(CandidateMethodInfo methodInfo, FacadeInterfaceInfo facadeInfo)
    {
        _methods.Add(methodInfo);
        _facadeInfo = facadeInfo;
    }

    /// <inheritdoc />
    public bool Validate(SourceProductionContext context)
    {
        if (_facadeInfo is not { } facadeInfo)
            return false;

        // Check method name conflicts
        var methodNameConflicts = _methods
            .Select(methodInfo => (methodInfo, methodName: MethodNamingStrategy.GetMethodName(facadeInfo, methodInfo)))
            .Where(x => ReservedNames.Contains(x.methodName))
            .ToList();

        // Check parameter name conflicts
        var parameterNameConflicts = _methods
            .SelectMany(methodInfo => methodInfo.MethodSymbol.Parameters
                .Where(parameter => ReservedNames.Contains(parameter.Name))
                .Select(parameter => (methodInfo, parameter)))
            .ToList();

        // Report method name conflicts
        foreach (var (methodInfo, methodName) in methodNameConflicts)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ConflictingGeneratedMemberName,
                methodInfo.MethodSymbol.Locations.FirstOrDefault(),
                methodName,
                methodInfo.MethodSymbol.Name));
        }

        // Report parameter name conflicts
        foreach (var (methodInfo, parameter) in parameterNameConflicts)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ConflictingGeneratedMemberName,
                parameter.Locations.FirstOrDefault() ?? methodInfo.MethodSymbol.Locations.FirstOrDefault(),
                parameter.Name,
                methodInfo.MethodSymbol.Name));
        }

        return methodNameConflicts.Count > 0 || parameterNameConflicts.Count > 0;
    }
}
