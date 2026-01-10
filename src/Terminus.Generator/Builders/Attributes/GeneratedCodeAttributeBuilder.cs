using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Attributes;

/// <summary>
/// Builds the [GeneratedCode] attribute for generated types.
/// </summary>
internal static class GeneratedCodeAttributeBuilder
{
    /// <summary>
    /// Creates a [GeneratedCode("Terminus.Generator", "version")] attribute.
    /// </summary>
    public static AttributeListSyntax Build()
    {
        var generatedCodeAttribute = Attribute(
            ParseName("global::System.CodeDom.Compiler.GeneratedCode"),
            AttributeArgumentList(SeparatedList([
                AttributeArgument(LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal("Terminus.Generator"))),
                AttributeArgument(LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal(GeneratorVersion.Version)))
            ])));

        return AttributeList(SingletonSeparatedList(generatedCodeAttribute));
    }
}
