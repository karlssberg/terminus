using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terminus.Generator;

internal class GenerationFeatures(AttributeData aggregatorAttrData)
{
    public bool IsScoped => ResolveNamedArgument<bool>("CreateScope");

    public string? CommandName => ResolveNamedArgument<string?>("CommandName");
    public string? QueryName => ResolveNamedArgument<string?>("QueryName");
    public string? AsyncCommandName => ResolveNamedArgument<string?>("AsyncCommandName");
    public string? AsyncQueryName => ResolveNamedArgument<string?>("AsyncQueryName");
    public string? AsyncStreamName => ResolveNamedArgument<string?>("AsyncStreamName");
    public int AggregationMode => ResolveNamedArgument<int>("AggregationMode");

    public ImmutableArray<INamedTypeSymbol> InterceptorTypes => ResolveInterceptorTypes();

    public bool HasInterceptors => !InterceptorTypes.IsDefaultOrEmpty;

    public MethodDiscoveryMode MethodDiscovery
    {
        get
        {
            // Try new MethodDiscovery property first (enum value)
            var methodDiscovery = ResolveNamedArgument<int?>("MethodDiscovery");
            if (methodDiscovery.HasValue)
                return (MethodDiscoveryMode)methodDiscovery.Value;

            // Fall back to legacy IncludeReferencedAssemblies boolean property
            var includeReferenced = ResolveNamedArgument<bool>("IncludeReferencedAssemblies");
            return includeReferenced ? MethodDiscoveryMode.TransitiveAssemblies : MethodDiscoveryMode.None;
        }
    }

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
        if (text.StartsWith("$@\"") || text.StartsWith("@$\""))
            startOffset = 3;
        else if (text.StartsWith("$\""))
            startOffset = 2;
        else if (text.StartsWith("$\"\"\""))
        {
            var dollarCount = text.TakeWhile(c => c == '$').Count();
            var quoteCount = text.Skip(dollarCount).TakeWhile(c => c == '"').Count();
            startOffset = dollarCount + quoteCount;
        }

        var endOffset = 0;
        if (text.EndsWith("\"\"\""))
        {
            endOffset = text.Reverse().TakeWhile(c => c == '"').Count();
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
        if (text.StartsWith("@\""))
            startOffset = 2;
        else if (text.StartsWith("\"\"\""))
            startOffset = text.TakeWhile(c => c == '"').Count();
        else if (text.StartsWith("\""))
            startOffset = 1;

        var endOffset = 0;
        if (text.EndsWith("\"\"\""))
            endOffset = text.Reverse().TakeWhile(c => c == '"').Count();
        else if (text.EndsWith("\""))
            endOffset = 1;

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

    private ImmutableArray<INamedTypeSymbol> ResolveInterceptorTypes()
    {
        var arg = aggregatorAttrData.NamedArguments
            .FirstOrDefault(a => a.Key == "Interceptors");

        if (arg.Value.IsNull || arg.Value.Kind != TypedConstantKind.Array)
            return ImmutableArray<INamedTypeSymbol>.Empty;

        return
        [
            ..arg.Value.Values
                .Where(v => v.Kind == TypedConstantKind.Type && v.Value != null)
                .Select(v => v.Value)
                .OfType<INamedTypeSymbol>()
        ];
    }
}