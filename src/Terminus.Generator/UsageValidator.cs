using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Terminus.Generator.Validation;

namespace Terminus.Generator;

internal static class UsageValidator
{
    internal static bool Validate(SourceProductionContext context, FacadeInterfaceInfo facadeInfo, ImmutableArray<CandidateMethodInfo> facadeMethodMethodInfos)
    {
        var invalidMethodNameValidator = new InvalidMethodNameValidator();
        var validator = new CompositeMethodValidator(
            invalidMethodNameValidator,
            new RefOrOutParameterValidator(),
            new DuplicateSignatureValidator(),
            new ConflictingNameValidator()
        );

        // Initialize the method name validator even if there are no methods
        invalidMethodNameValidator.Initialize(facadeInfo);

        foreach (var facadeMethod in facadeMethodMethodInfos)
        {
            validator.Add(facadeMethod, facadeInfo);
        }

        return validator.Validate(context);
    }
}