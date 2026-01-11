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
        var hasErrors = false;

        if (_facadeInfo is not { } facadeInfo)
            return false;

        foreach (var methodInfo in _methods)
        {
            var methodName = MethodNamingStrategy.GetMethodName(facadeInfo, methodInfo);
            if (ReservedNames.Contains(methodName))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ConflictingGeneratedMemberName,
                    methodInfo.MethodSymbol.Locations.FirstOrDefault(),
                    methodName,
                    methodInfo.MethodSymbol.Name));
                hasErrors = true;
            }

            foreach (var parameter in methodInfo.MethodSymbol.Parameters)
            {
                if (ReservedNames.Contains(parameter.Name))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.ConflictingGeneratedMemberName,
                        parameter.Locations.FirstOrDefault() ?? methodInfo.MethodSymbol.Locations.FirstOrDefault(),
                        parameter.Name,
                        methodInfo.MethodSymbol.Name));
                    hasErrors = true;
                }
            }
        }

        return hasErrors;
    }
}
