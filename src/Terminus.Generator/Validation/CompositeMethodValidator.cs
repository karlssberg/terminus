using Microsoft.CodeAnalysis;

namespace Terminus.Generator.Validation;

/// <summary>
/// A composite validator that aggregates multiple <see cref="IMethodValidator"/> instances
/// and executes them in sequence.
/// </summary>
/// <param name="validators">The collection of validators to be executed.</param>
internal sealed class CompositeMethodValidator(params IMethodValidator[] validators) : IMethodValidator
{
    /// <inheritdoc />
    public void Validate(CandidateMethodInfo methodInfo)
    {
        foreach (var validator in validators)
        {
            validator.Validate(methodInfo);
        }
    }

    /// <inheritdoc />
    public void Finalize(SourceProductionContext context, ref bool hasErrors)
    {
        foreach (var validator in validators)
        {
            validator.Finalize(context, ref hasErrors);
        }
    }
}
