using Microsoft.CodeAnalysis;
using static Terminus.Generator.Diagnostics;

namespace Terminus.Generator.Validation;

/// <summary>
/// Validates that methods do not have unsupported 'ref' or 'out' parameters.
/// </summary>
internal class RefOrOutParameterValidator : IMethodValidator
{
    private readonly List<CandidateMethodInfo> _methods = [];

    /// <inheritdoc />
    public void Add(CandidateMethodInfo methodInfo, FacadeInterfaceInfo facadeInfo)
    {
        _methods.Add(methodInfo);
    }

    /// <inheritdoc />
    public bool Validate(SourceProductionContext context)
    {
        var hasErrors = false;
        var methodSymbols = _methods
            .Select(methodInfo => methodInfo.MethodSymbol);
        
        var diagnostics = 
            from methodSymbol in methodSymbols
            from parameter in methodSymbol.Parameters
            where IsRefOrOut(parameter) 
            select CreateNoRefOrOutParameterDiagnostic(methodSymbol, parameter);
                
        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
            hasErrors = true;
        }

        return hasErrors;
    }

    private Diagnostic CreateNoRefOrOutParameterDiagnostic(IMethodSymbol methodSymbol, IParameterSymbol parameter)
    {
        var location = parameter.Locations.FirstOrDefault()
                       ?? methodSymbol.Locations.FirstOrDefault();
        
        return Diagnostic.Create(
            RefOrOutParameter,
            location, 
            methodSymbol.Name, 
            parameter.Name);
    }

    private static bool IsRefOrOut(IParameterSymbol p) => p.RefKind is RefKind.Ref or RefKind.Out;
}
