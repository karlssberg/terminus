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
        Array.ForEach(validators, v => v.Add(methodInfo, facadeInfo));
    }

    /// <inheritdoc />
    public bool Validate(SourceProductionContext context)
    {
        return validators.Aggregate(false, (hasErrors, validator) => validator.Validate(context) || hasErrors);
    }
}
