using Microsoft.CodeAnalysis;

namespace Terminus.Generator.Validation;

/// <summary>
/// Validates that custom method names specified in FacadeOfAttribute are valid C# identifiers.
/// </summary>
internal class InvalidMethodNameValidator : IMethodValidator
{
    private FacadeInterfaceInfo? _facadeInfo;

    /// <inheritdoc />
    public void Add(CandidateMethodInfo methodInfo, FacadeInterfaceInfo facadeInfo)
    {
        // Store the facade info - we only need it once since validation is at the facade level
        _facadeInfo ??= facadeInfo;
    }

    /// <summary>
    /// Initializes the validator with facade info (for cases where no methods exist).
    /// </summary>
    public void Initialize(FacadeInterfaceInfo facadeInfo)
    {
        _facadeInfo ??= facadeInfo;
    }

    /// <inheritdoc />
    public bool Validate(SourceProductionContext context)
    {
        if (_facadeInfo is not { } facadeInfo)
            return false;

        var hasErrors = false;
        var features = facadeInfo.Features;
        var interfaceName = facadeInfo.InterfaceSymbol.Name;

        // Validate each method name property
        hasErrors |= ValidateMethodName(context, features.CommandName, "CommandName", interfaceName);
        hasErrors |= ValidateMethodName(context, features.QueryName, "QueryName", interfaceName);
        hasErrors |= ValidateMethodName(context, features.AsyncCommandName, "AsyncCommandName", interfaceName);
        hasErrors |= ValidateMethodName(context, features.AsyncQueryName, "AsyncQueryName", interfaceName);
        hasErrors |= ValidateMethodName(context, features.AsyncStreamName, "AsyncStreamName", interfaceName);

        return hasErrors;
    }

    private bool ValidateMethodName(
        SourceProductionContext context,
        string? methodName,
        string propertyName,
        string interfaceName)
    {
        // Only validate if a name was explicitly provided
        if (methodName == null)
            return false;

        // Check if it's a valid identifier
        if (IdentifierValidator.IsValidIdentifier(methodName))
            return false;

        // Get the precise location of the argument value, or fall back to interface location
        var location = _facadeInfo?.Features.GetNamedArgumentLocation(propertyName);

        // Report diagnostic for invalid method name
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.InvalidMethodName,
            location,
            methodName,
            propertyName,
            interfaceName));

        return true;
    }
}
