using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal class GenerationFeatures(AttributeData aggregatorAttrData)
{

    public bool IsScoped => ResolveNamedArgument<bool>("Scoped");
    public string? CommandName => ResolveNamedArgument<string?>("CommandName");
    public string? QueryName => ResolveNamedArgument<string?>("QueryName");
    public string? AsyncCommandName => ResolveNamedArgument<string?>("AsyncCommandName");
    public string? AsyncQueryName => ResolveNamedArgument<string?>("AsyncQueryName");
    
    public string? AsyncStreamName => ResolveNamedArgument<string?>("AsyncStreamName");

    private T? ResolveNamedArgument<T>(string name)
    {
        var arg = aggregatorAttrData.NamedArguments
            .FirstOrDefault(arg => arg.Key == name);

        if (arg.Value.IsNull)
        {
            return default;
        }

        return (T?)arg.Value.Value;
    }
}