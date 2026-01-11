using Microsoft.CodeAnalysis;

namespace Terminus.Generator.Validation;

/// <summary>
/// Defines a validator for checking the validity of discovered candidate methods before code generation.
/// </summary>
internal interface IMethodValidator
{
    /// <summary>
    /// Performs validation logic for a single candidate method.
    /// This may involve collecting state for cross-method validation.
    /// </summary>
    /// <param name="methodInfo">The metadata for the candidate method being validated.</param>
    void Add(CandidateMethodInfo methodInfo);

    /// <summary>
    /// Validate the accumulated methods, reporting any errors via the provided context.
    /// </summary>
    /// <param name="context">The source production context used to report diagnostics.</param>
    /// <returns>True if any errors were detected; otherwise, false.</returns>
    bool Validate(SourceProductionContext context);
}
