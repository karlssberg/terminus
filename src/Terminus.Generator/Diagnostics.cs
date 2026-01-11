using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor DuplicateFacadeMethodSignature = new(
        id: "TM0001",
        title: "Duplicate entry point signature",
        messageFormat: "Duplicate entry point signature detected for method '{0}'",
        category: "Terminus.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Entry point methods must have unique signatures within the same attribute type.");

    public static readonly DiagnosticDescriptor RefOrOutParameter = new(
        id: "TM0002",
        title: "Ref or out parameter in entry point",
        messageFormat: "Entry point method '{0}' cannot have ref or out parameters (Parameter '{1}' is invalid)",
        category: "Terminus.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Entry point methods cannot have ref or out parameters as they cannot be properly resolved by the parameter binding system.");
}
