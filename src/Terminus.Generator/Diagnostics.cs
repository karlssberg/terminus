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
        helpLinkUri: "https://github.com/karlssberg/terminus/blob/main/docs/diagnostics/TM0001.md",
        description: "Entry point methods must have unique signatures within the same attribute type.");

    public static readonly DiagnosticDescriptor RefOrOutParameter = new(
        id: "TM0002",
        title: "Unsupported parameter modifier in entry point",
        messageFormat: "Entry point method '{0}' cannot have ref, out or in parameters (Parameter '{1}' is invalid)",
        category: "Terminus.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/danielkarlsson/terminus/blob/main/docs/diagnostics/TM0002.md",
        description: "Entry point methods cannot have ref, out or in parameters as they cannot be properly resolved by the parameter binding system.");

    public static readonly DiagnosticDescriptor ConflictingGeneratedMemberName = new(
        id: "TM0003",
        title: "Method or parameter name conflicts with generated members",
        messageFormat: "Member or parameter '{0}' in method '{1}' conflicts with a name used in the generated facade implementation",
        category: "Terminus.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/danielkarlsson/terminus/blob/main/docs/diagnostics/TM0003.md",
        description: "Generated facade implementations use internal field names like _serviceProvider, _syncScope, etc. These names must not be used for entry point methods or their parameters.");

    public static readonly DiagnosticDescriptor InvalidMethodName = new(
        id: "TM0004",
        title: "Invalid method name in FacadeOf attribute",
        messageFormat: "Invalid method name '{0}' specified for '{1}' in facade '{2}'. Method names must be valid C# identifiers.",
        category: "Terminus.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/karlssberg/terminus/blob/main/docs/diagnostics/TM0004.md",
        description: "Custom method names specified in FacadeOfAttribute properties (CommandName, QueryName, AsyncCommandName, AsyncQueryName, AsyncStreamName) must be valid C# identifiers.");

    public static readonly DiagnosticDescriptor DuplicatePropertyName = new(
        id: "TM0005",
        title: "Duplicate property name in facade",
        messageFormat: "Property '{0}' is declared multiple times in facade",
        category: "Terminus.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/karlssberg/terminus/blob/main/docs/diagnostics/TM0005.md",
        description: "Facade properties must have unique names. Unlike methods which can be aggregated, properties represent state and cannot be combined.");

    public static readonly DiagnosticDescriptor MethodPropertyNameConflict = new(
        id: "TM0006",
        title: "Method name conflicts with property name",
        messageFormat: "Method '{0}' conflicts with property '{1}' in facade '{2}'",
        category: "Terminus.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/karlssberg/terminus/blob/main/docs/diagnostics/TM0006.md",
        description: "Facade methods and properties must have unique names.");

    public static readonly DiagnosticDescriptor IncompatibleReturnTypesInAggregation = new(
        id: "TM0007",
        title: "Incompatible return types in aggregated methods",
        messageFormat: "Methods with signature '{0}' have incompatible return types: '{1}' and '{2}'",
        category: "Terminus.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/karlssberg/terminus/blob/main/docs/diagnostics/TM0007.md",
        description: "Aggregated methods must have compatible return types.");

    public static readonly DiagnosticDescriptor MultipleHandlersWithoutAggregation = new(
        id: "TM0008",
        title: "Multiple handlers with same signature when aggregation is disabled",
        messageFormat: "Multiple handlers with signature '{0}' found when AggregationMode is None. Set an AggregationMode flag to enable aggregation for this return type.",
        category: "Terminus.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/karlssberg/terminus/blob/main/docs/diagnostics/TM0008.md",
        description: "When AggregationMode is None (default), each method signature must have exactly one handler. To allow multiple handlers with the same signature, set an appropriate AggregationMode flag.");

    public static readonly DiagnosticDescriptor MultipleHandlersWithFirstStrategy = new(
        id: "TM0010",
        title: "Multiple handlers with First strategy",
        messageFormat: "Multiple handlers detected for method '{0}' but AggregationReturnTypeStrategy is set to First. Only the first handler ('{1}') will be executed. Consider using Collection strategy or ensuring only one handler exists.",
        category: "Terminus.Generator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/karlssberg/terminus/blob/main/docs/diagnostics/TM0010.md",
        description: "When AggregationReturnTypeStrategy is First, only the first handler in an aggregated method group will be executed. This warning alerts when multiple handlers are detected.");

    public static readonly DiagnosticDescriptor NoClosedGenericInstantiationsFound = new(
        id: "TM0011",
        title: "No closed generic instantiations found for open generic type",
        messageFormat: "Open generic type '{0}' with facade method attributes has no closed generic instantiations in the compilation. Ensure the type is used with concrete type arguments.",
        category: "Terminus.Generator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/karlssberg/terminus/blob/main/docs/diagnostics/TM0011.md",
        description: "Open generic types with facade method attributes must have at least one closed generic instantiation (e.g., via implementation, field usage, or type parameter) for methods to be discovered.");

    public static readonly DiagnosticDescriptor TypeParameterConstraintViolation = new(
        id: "TM0012",
        title: "Type parameter constraint violation in closed generic",
        messageFormat: "Closed generic type '{0}' violates type parameter constraints from open generic '{1}': {2}",
        category: "Terminus.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/karlssberg/terminus/blob/main/docs/diagnostics/TM0012.md",
        description: "Type arguments in closed generic types must satisfy the constraints defined on the open generic type's type parameters.");
}
