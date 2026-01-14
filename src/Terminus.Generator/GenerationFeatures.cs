using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    public Location? GetNamedArgumentLocation(string name)
    {
        var arg = aggregatorAttrData.NamedArguments
            .FirstOrDefault(arg => arg.Key == name);

        if (arg.Value.IsNull)
            return null;

        // Get the syntax node for the attribute
        var attributeSyntax = aggregatorAttrData.ApplicationSyntaxReference?.GetSyntax();
        if (attributeSyntax is not AttributeSyntax attrSyntax)
            return null;

        // Find the argument with the matching name
        var argumentSyntax = attrSyntax.ArgumentList?.Arguments
            .FirstOrDefault(a => a.NameEquals?.Name.Identifier.Text == name);

        if (argumentSyntax?.NameEquals == null || argumentSyntax.Expression is not LiteralExpressionSyntax literalExpr)
            return argumentSyntax?.Expression.GetLocation();
        
        var literalToken = literalExpr.Token;
        var literalLocation = literalToken.GetLocation();
        
        // Adjust location to exclude the opening quote for string literals
        if (literalToken.Text.StartsWith("\"") && literalToken.Text.Length > 1)
        {
            var sourceTree = literalLocation.SourceTree;
            var span = literalLocation.SourceSpan;
            var adjustedSpan = new Microsoft.CodeAnalysis.Text.TextSpan(span.Start + 1, span.Length - 2);
            return Location.Create(sourceTree!, adjustedSpan);
        }
        
        return literalLocation;
    }

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