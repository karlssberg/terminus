using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal class GenerationFeatures(AttributeData aggregatorAttrData, bool isOfficialTerminusAttribute)
{

    public bool IsScoped => ResolveIsScoped();

    private bool ResolveIsScoped()
    {
        // Check if Scoped named argument exists
        var scopedArg = aggregatorAttrData.NamedArguments
            .FirstOrDefault(arg => arg.Key == "Scoped");

        // If Scoped was explicitly set, use that value
        if (!scopedArg.Equals(default(KeyValuePair<string, TypedConstant>)))
        {
            return scopedArg.Value.Value is true;
        }

        // For official Terminus.FacadeOfAttribute, default to true (include Dispatcher)
        // For user-defined attributes, default to false (don't include Dispatcher)
        return isOfficialTerminusAttribute;
    }
}