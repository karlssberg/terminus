using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Terminus.Generator.Validation;

namespace Terminus.Generator;

internal static class UsageValidator
{
    internal static bool Validate(
        SourceProductionContext context,
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<CandidateMethodInfo> facadeMethodMethodInfos)
    {
        return Validate(context, facadeInfo, facadeMethodMethodInfos, ImmutableArray<CandidatePropertyInfo>.Empty);
    }

    internal static bool Validate(
        SourceProductionContext context,
        FacadeInterfaceInfo facadeInfo,
        ImmutableArray<CandidateMethodInfo> facadeMethodMethodInfos,
        ImmutableArray<CandidatePropertyInfo> properties)
    {
        var invalidMethodNameValidator = new InvalidMethodNameValidator();
        var methodPropertyConflictValidator = new MethodPropertyConflictValidator();
        methodPropertyConflictValidator.SetProperties(properties);

        var validator = new CompositeMethodValidator(
            invalidMethodNameValidator,
            new RefOrOutParameterValidator(),
            new DuplicateSignatureValidator(),
            new ConflictingNameValidator(),
            methodPropertyConflictValidator
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