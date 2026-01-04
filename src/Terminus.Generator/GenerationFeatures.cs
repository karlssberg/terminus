using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal class GenerationFeatures(AttributeData aggregatorAttrData)
{

    public bool IsScoped => ResolveIsScoped();
        
    private bool ResolveIsScoped()
    {
        var typedConstant = aggregatorAttrData.NamedArguments
            .FirstOrDefault(arg => arg.Key == "Scoped").Value;
        
        return typedConstant.Value is true;
    }
}