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

        return scopedArg.Value.Value is true;
    }
}