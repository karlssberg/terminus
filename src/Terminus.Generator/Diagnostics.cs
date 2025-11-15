using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor DuplicateEntryPointSignature = new(
        id: "TM0001",
        title: "Duplicate entry point signature",
        messageFormat: "Duplicate entry point signature detected for method '{0}'",
        category: "Terminus.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Entry point methods must have unique signatures within the same attribute type.");

    public static readonly DiagnosticDescriptor GenericEntryPointMethod = new(
        id: "TM0002",
        title: "Generic entry point method in mediator",
        messageFormat: "Entry point method '{0}' cannot have generic type parameters when used in a mediator",
        category: "Terminus.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Mediator entry point methods cannot be generic. Facade entry points can be generic.");

    public static readonly DiagnosticDescriptor RefOrOutParameter = new(
        id: "TM0003",
        title: "Ref or out parameter in entry point",
        messageFormat: "Entry point method '{0}' cannot have ref or out parameters (Parameter '{1}' is invalid)",
        category: "Terminus.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Entry point methods cannot have ref or out parameters as they cannot be properly resolved by the parameter binding system.");
}
