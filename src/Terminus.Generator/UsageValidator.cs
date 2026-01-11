using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Terminus.Generator.Validation;

namespace Terminus.Generator;

internal static class UsageValidator
{
    internal static bool Validate(SourceProductionContext context, FacadeInterfaceInfo facadeInfo, ImmutableArray<CandidateMethodInfo> facadeMethodMethodInfos)
    {
        var validator = new CompositeMethodValidator(
            new RefOrOutParameterValidator(),
            new DuplicateSignatureValidator()
        );

        foreach (var facadeMethod in facadeMethodMethodInfos)
        {
            validator.Add(facadeMethod, facadeInfo);
        }

        return validator.Validate(context);
    }
}