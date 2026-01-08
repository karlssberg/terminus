using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Class;

/// <summary>
/// Builds disposal method declarations (Dispose/DisposeAsync) for scoped facades.
/// </summary>
internal static class DisposalBuilder
{
    /// <summary>
    /// Builds both Dispose and DisposeAsync methods for scoped facades.
    /// </summary>
    public static IEnumerable<MemberDeclarationSyntax> BuildDisposalMethods()
    {
        yield return ParseMemberDeclaration(
            """
            public void Dispose()
            {
                if (_syncDisposed || !_syncScope.IsValueCreated) return;

                _syncScope.Value.Dispose();
                _syncDisposed = true;

                global::System.GC.SuppressFinalize(this);
            }
            """)!;

        yield return ParseMemberDeclaration(
            """
            public async global::System.Threading.Tasks.ValueTask DisposeAsync()
            {
                if (_asyncDisposed || !_asyncScope.IsValueCreated) return;

                await _asyncScope.Value.DisposeAsync().ConfigureAwait(false);
                _asyncDisposed = true;

                global::System.GC.SuppressFinalize(this);
            }
            """)!;
    }
}
