using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal class GenerationFeatures(AttributeData aggregatorAttrData)
{
    public bool IsScoped
    {
        get
        {
            // Try new Lifetime property first (enum value 1 = Scoped)
            var lifetime = ResolveNamedArgument<int?>("Lifetime");
            if (lifetime.HasValue)
                return lifetime.Value == 1; // FacadeLifetime.Scoped

            // Fall back to legacy Scoped boolean property
            return ResolveNamedArgument<bool>("Scoped");
        }
    }

    public string? CommandName => ResolveNamedArgument<string?>("CommandName");
    public string? QueryName => ResolveNamedArgument<string?>("QueryName");
    public string? AsyncCommandName => ResolveNamedArgument<string?>("AsyncCommandName");
    public string? AsyncQueryName => ResolveNamedArgument<string?>("AsyncQueryName");
    public string? AsyncStreamName => ResolveNamedArgument<string?>("AsyncStreamName");
    public int AggregationMode => ResolveNamedArgument<int>("AggregationMode");

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