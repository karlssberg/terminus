using Microsoft.CodeAnalysis;

namespace Terminus.Generator.Validation;

/// <summary>
/// Validates that methods do not have unsupported 'ref' or 'out' parameters.
/// </summary>
/// <param name="context">The source production context used to report diagnostics.</param>
internal class RefOrOutParameterValidator(SourceProductionContext context) : IMethodValidator
{
    private bool _hasErrors;

    /// <inheritdoc />
    public void Validate(CandidateMethodInfo methodInfo)
    {
        var refOrOutParameters = methodInfo.MethodSymbol
            .Parameters.Where(p => p.RefKind is RefKind.Ref or RefKind.Out);

        foreach (var parameter in refOrOutParameters)
        {
            var diagnostic = Diagnostic.Create(
                Diagnostics.RefOrOutParameter,
                parameter.Locations.FirstOrDefault() ?? methodInfo.MethodSymbol.Locations.FirstOrDefault(),
                methodInfo.MethodSymbol.Name,
                parameter.Name);
            
            context.ReportDiagnostic(diagnostic);
            _hasErrors = true;
        }
    }

    /// <inheritdoc />
    public void Finalize(SourceProductionContext _, ref bool hasErrors)
    {
        if (_hasErrors)
        {
            hasErrors = true;
        }
    }
}
