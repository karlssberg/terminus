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
}
