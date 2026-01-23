using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Class;

/// <summary>
/// Builds field declarations for facade implementation classes.
/// </summary>
internal static class FieldBuilder
{
    /// <summary>
    /// Builds fields required for non-scoped facades.
    /// </summary>
    public static IEnumerable<MemberDeclarationSyntax> BuildNonScopedFields()
    {
        yield return ParseMemberDeclaration("private readonly global::System.IServiceProvider _serviceProvider;")!;
    }

    /// <summary>
    /// Builds fields required for scoped facades with instance methods.
    /// </summary>
    public static IEnumerable<MemberDeclarationSyntax> BuildScopedFields()
    {
        yield return ParseMemberDeclaration("private bool _syncDisposed;")!;
        yield return ParseMemberDeclaration("private bool _asyncDisposed;")!;
        yield return ParseMemberDeclaration("private readonly global::System.Lazy<global::Microsoft.Extensions.DependencyInjection.IServiceScope> _syncScope;")!;
        yield return ParseMemberDeclaration("private readonly global::System.Lazy<global::Microsoft.Extensions.DependencyInjection.AsyncServiceScope> _asyncScope;")!;
    }

    /// <summary>
    /// Builds the interceptors field when interceptors are configured.
    /// </summary>
    public static MemberDeclarationSyntax BuildInterceptorsField()
    {
        return ParseMemberDeclaration("private readonly global::Terminus.IFacadeInterceptor[] _interceptors;")!;
    }
}
