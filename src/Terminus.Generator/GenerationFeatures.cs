using Microsoft.CodeAnalysis;
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

        var argumentSyntax = GetArgumentSyntax(name);
        if (argumentSyntax == null)
            return null;

        return argumentSyntax.Expression switch
        {
            LiteralExpressionSyntax literalExpr => GetLiteralLocation(literalExpr),
            InterpolatedStringExpressionSyntax interpolatedString => GetInterpolatedStringLocation(interpolatedString),
            _ => argumentSyntax.Expression.GetLocation()
        };
    }

    private AttributeArgumentSyntax? GetArgumentSyntax(string name)
    {
        var attributeSyntax = aggregatorAttrData.ApplicationSyntaxReference?.GetSyntax();
        if (attributeSyntax is not AttributeSyntax attrSyntax)
            return null;

        return attrSyntax.ArgumentList?.Arguments
            .FirstOrDefault(a => a.NameEquals?.Name.Identifier.Text == name);
    }

    private static Location GetInterpolatedStringLocation(InterpolatedStringExpressionSyntax interpolatedString)
    {
        var location = interpolatedString.GetLocation();
        var text = interpolatedString.ToString();

        var startOffset = 0;
        if (text.StartsWith("$@\"")) startOffset = 3;
        else if (text.StartsWith("@$\"")) startOffset = 3;
        else if (text.StartsWith("$\"")) startOffset = 2;
        else if (text.StartsWith("$\"\"\""))
        {
            var dollarCount = 0;
            while (dollarCount < text.Length && text[dollarCount] == '$') dollarCount++;
            var quoteCount = 0;
            while (dollarCount + quoteCount < text.Length && text[dollarCount + quoteCount] == '"') quoteCount++;
            startOffset = dollarCount + quoteCount;
        }

        var endOffset = 0;
        if (text.EndsWith("\"\"\""))
        {
            while (endOffset < text.Length && text[text.Length - 1 - endOffset] == '"') endOffset++;
        }
        else if (text.EndsWith("\""))
        {
            endOffset = 1;
        }

        if (startOffset > 0 || endOffset > 0)
        {
            var span = location.SourceSpan;
            var newLength = Math.Max(0, span.Length - startOffset - endOffset);
            var adjustedSpan = new Microsoft.CodeAnalysis.Text.TextSpan(span.Start + startOffset, newLength);
            return Location.Create(location.SourceTree!, adjustedSpan);
        }

        return location;
    }

    private static Location GetLiteralLocation(LiteralExpressionSyntax literalExpr)
    {
        var literalToken = literalExpr.Token;
        var literalLocation = literalToken.GetLocation();
        var text = literalToken.Text;

        var startOffset = 0;
        if (text.StartsWith("@\"")) startOffset = 2;
        else if (text.StartsWith("\"\"\""))
        {
            while (startOffset < text.Length && text[startOffset] == '"') startOffset++;
        }
        else if (text.StartsWith("\""))
        {
            startOffset = 1;
        }

        var endOffset = 0;
        if (text.EndsWith("\"\"\""))
        {
            while (endOffset < text.Length && text[text.Length - 1 - endOffset] == '"') endOffset++;
        }
        else if (text.EndsWith("\""))
        {
            endOffset = 1;
        }

        if (startOffset > 0 || endOffset > 0)
        {
            var span = literalLocation.SourceSpan;
            var newLength = Math.Max(0, span.Length - startOffset - endOffset);
            var adjustedSpan = new Microsoft.CodeAnalysis.Text.TextSpan(span.Start + startOffset, newLength);
            return Location.Create(literalLocation.SourceTree!, adjustedSpan);
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