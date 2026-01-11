using System.Collections.Generic;
using System.Linq;
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
            .SelectMany(
                methodInfo =>  methodInfo.MethodSymbol.Parameters.Where(IsRefOrOut),
                (methodInfo, parameter) => 
                    Diagnostic.Create(
                        RefOrOutParameter,
                        ResolveLocation(parameter, methodInfo), 
                        methodInfo.MethodSymbol.Name, 
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

        Location? ResolveLocation(IParameterSymbol parameter, CandidateMethodInfo methodInfo)
        {
            return parameter.Locations.FirstOrDefault()
                   ?? methodInfo.MethodSymbol.Locations.FirstOrDefault();
        }
    }
}
