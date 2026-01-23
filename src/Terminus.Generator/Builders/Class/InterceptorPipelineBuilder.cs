using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Class;

/// <summary>
/// Builds interceptor pipeline method declarations for facade implementation classes.
/// </summary>
internal static class InterceptorPipelineBuilder
{
    /// <summary>
    /// Builds the synchronous interceptor pipeline method.
    /// </summary>
    public static MemberDeclarationSyntax BuildSyncPipelineMethod()
    {
        return ParseMemberDeclaration(
            """
            private TResult? ExecuteWithInterceptors<TResult>(global::Terminus.FacadeInvocationContext context, global::Terminus.FacadeInvocationDelegate<TResult> target)
            {
                var index = 0;
                global::Terminus.FacadeInvocationDelegate<TResult> BuildPipeline()
                {
                    if (index >= _interceptors.Length)
                        return target;
                    var currentIndex = index++;
                    var next = BuildPipeline();
                    return () => _interceptors[currentIndex].Intercept(context, next);
                }

                return BuildPipeline()();
            }
            """)!;
    }

    /// <summary>
    /// Builds the asynchronous interceptor pipeline method.
    /// </summary>
    public static MemberDeclarationSyntax BuildAsyncPipelineMethod()
    {
        return ParseMemberDeclaration(
            """
            private async global::System.Threading.Tasks.ValueTask<TResult?> ExecuteWithInterceptorsAsync<TResult>(global::Terminus.FacadeInvocationContext context, global::Terminus.FacadeAsyncInvocationDelegate<TResult> target)
            {
                var index = 0;
                global::Terminus.FacadeAsyncInvocationDelegate<TResult> BuildPipeline()
                {
                    if (index >= _interceptors.Length)
                        return target;
                    var currentIndex = index++;
                    var next = BuildPipeline();
                    return () => _interceptors[currentIndex].InterceptAsync(context, next);
                }

                return await BuildPipeline()().ConfigureAwait(false);
            }
            """)!;
    }

    /// <summary>
    /// Builds the streaming interceptor pipeline method.
    /// </summary>
    public static MemberDeclarationSyntax BuildStreamPipelineMethod()
    {
        return ParseMemberDeclaration(
            """
            private global::System.Collections.Generic.IAsyncEnumerable<TItem> ExecuteWithInterceptorsStream<TItem>(global::Terminus.FacadeInvocationContext context, global::Terminus.FacadeStreamInvocationDelegate<TItem> target)
            {
                var index = 0;
                global::Terminus.FacadeStreamInvocationDelegate<TItem> BuildPipeline()
                {
                    if (index >= _interceptors.Length)
                        return target;
                    var currentIndex = index++;
                    var next = BuildPipeline();
                    return () => _interceptors[currentIndex].InterceptStream(context, next);
                }

                return BuildPipeline()();
            }
            """)!;
    }
}
