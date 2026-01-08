using Microsoft.CodeAnalysis;

namespace Terminus.Generator.Builders.Core;

/// <summary>
/// Base interface for all code builders in the generator.
/// </summary>
/// <typeparam name="TContext">The context type required by this builder</typeparam>
internal interface ICodeBuilder<in TContext>
{
    /// <summary>
    /// Builds a syntax node from the given context.
    /// </summary>
    SyntaxNode Build(TContext context);
}
