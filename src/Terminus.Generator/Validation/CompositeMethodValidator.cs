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
    public void Add(CandidateMethodInfo methodInfo, FacadeInterfaceInfo facadeInfo)
    {
        foreach (var validator in validators)
        {
            validator.Add(methodInfo, facadeInfo);
        }
    }

    /// <inheritdoc />
    public bool Validate(SourceProductionContext context)
    {
        var hasErrors = false;
        foreach (var validator in validators)
        {
            if (validator.Validate(context))
            {
                hasErrors = true;
            }
        }

        return hasErrors;
    }
}
