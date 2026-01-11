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
    public void Add(CandidateMethodInfo methodInfo)
    {
        _methods.Add(methodInfo);
    }

    /// <inheritdoc />
    public void Validate(SourceProductionContext context, ref bool hasErrors)
    {
        var diagnostics = _methods
            .Select(methodInfo => methodInfo.MethodSymbol)
            .SelectMany(
                methodSymbol =>  methodSymbol.Parameters.Where(IsRefOrOut),
                (methodSymbol, parameter) => 
                    Diagnostic.Create(
                        RefOrOutParameter,
                        ResolveLocation(parameter, methodSymbol), 
                        methodSymbol.Name, 
                        parameter.Name));
                
        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
            hasErrors = true;
        }

        return;

        bool IsRefOrOut(IParameterSymbol p)
        {
            return p.RefKind is RefKind.Ref or RefKind.Out;
        }

        Location? ResolveLocation(IParameterSymbol parameter, IMethodSymbol methodSymbol)
        {
            return parameter.Locations.FirstOrDefault()
                   ?? methodSymbol.Locations.FirstOrDefault();
        }
    }
}
