using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Class;

/// <summary>
/// Builds interceptor pipeline method declarations for facade implementation classes.
/// </summary>
internal static class InterceptorPipelineBuilder
{
    /// <summary>
    /// Builds the synchronous void interceptor pipeline method.
    /// </summary>
    public static MemberDeclarationSyntax BuildSyncVoidPipelineMethod()
    {
        return ParseMemberDeclaration(
            """
            private void ExecuteWithVoidInterceptors(global::Terminus.FacadeInvocationContext context, global::Terminus.FacadeVoidInvocationDelegate target)
            {
                var index = 0;
                global::Terminus.FacadeVoidInvocationDelegate BuildPipeline()
                {
                    if (index >= _interceptors.Length)
                        return target;
                    var currentIndex = index++;
                    var next = BuildPipeline();
                    if (_interceptors[currentIndex] is global::Terminus.ISyncVoidFacadeInterceptor syncVoid)
                        return handlers => syncVoid.Intercept(context, nextHandlers => next(nextHandlers ?? handlers));
                    return next;
                }

                BuildPipeline()(null);
            }
            """)!;
    }

    /// <summary>
    /// Builds the synchronous result interceptor pipeline method.
    /// </summary>
    public static MemberDeclarationSyntax BuildSyncPipelineMethod()
    {
        return ParseMemberDeclaration(
            """
            private TResult ExecuteWithInterceptors<TResult>(global::Terminus.FacadeInvocationContext context, global::Terminus.FacadeInvocationDelegate<TResult> target)
            {
                var index = 0;
                global::Terminus.FacadeInvocationDelegate<TResult> BuildPipeline()
                {
                    if (index >= _interceptors.Length)
                        return target;
                    var currentIndex = index++;
                    var next = BuildPipeline();
                    if (_interceptors[currentIndex] is global::Terminus.ISyncFacadeInterceptor sync)
                        return handlers => sync.Intercept(context, nextHandlers => next(nextHandlers ?? handlers));
                    return next;
                }

                return BuildPipeline()(null);
            }
            """)!;
    }

    /// <summary>
    /// Builds the asynchronous void interceptor pipeline method.
    /// </summary>
    public static MemberDeclarationSyntax BuildAsyncVoidPipelineMethod()
    {
        return ParseMemberDeclaration(
            """
            private async global::System.Threading.Tasks.Task ExecuteWithAsyncVoidInterceptors(global::Terminus.FacadeInvocationContext context, global::Terminus.FacadeAsyncVoidInvocationDelegate target)
            {
                var index = 0;
                global::Terminus.FacadeAsyncVoidInvocationDelegate BuildPipeline()
                {
                    if (index >= _interceptors.Length)
                        return target;
                    var currentIndex = index++;
                    var next = BuildPipeline();
                    if (_interceptors[currentIndex] is global::Terminus.IAsyncVoidFacadeInterceptor asyncVoid)
                        return handlers => asyncVoid.InterceptAsync(context, nextHandlers => next(nextHandlers ?? handlers));
                    return next;
                }

                await BuildPipeline()(null).ConfigureAwait(false);
            }
            """)!;
    }

    /// <summary>
    /// Builds the asynchronous result interceptor pipeline method.
    /// </summary>
    public static MemberDeclarationSyntax BuildAsyncPipelineMethod()
    {
        return ParseMemberDeclaration(
            """
            private async global::System.Threading.Tasks.ValueTask<TResult> ExecuteWithInterceptorsAsync<TResult>(global::Terminus.FacadeInvocationContext context, global::Terminus.FacadeAsyncInvocationDelegate<TResult> target)
            {
                var index = 0;
                global::Terminus.FacadeAsyncInvocationDelegate<TResult> BuildPipeline()
                {
                    if (index >= _interceptors.Length)
                        return target;
                    var currentIndex = index++;
                    var next = BuildPipeline();
                    if (_interceptors[currentIndex] is global::Terminus.IAsyncFacadeInterceptor async)
                        return handlers => async.InterceptAsync(context, nextHandlers => next(nextHandlers ?? handlers));
                    return next;
                }

                return await BuildPipeline()(null).ConfigureAwait(false);
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
                    if (_interceptors[currentIndex] is global::Terminus.IStreamFacadeInterceptor stream)
                        return handlers => stream.InterceptStream(context, nextHandlers => next(nextHandlers ?? handlers));
                    return next;
                }

                return BuildPipeline()(null);
            }
            """)!;
    }

    /// <summary>
    /// Builds the synchronous void interceptor pipeline method for generic facades.
    /// </summary>
    public static MemberDeclarationSyntax BuildGenericSyncVoidPipelineMethod(string attributeTypeName)
    {
        return ParseMemberDeclaration(
            $$"""
            private void ExecuteWithVoidInterceptors(global::Terminus.FacadeInvocationContext<{{attributeTypeName}}> context, global::Terminus.FacadeVoidInvocationDelegate target)
            {
                var index = 0;
                global::Terminus.FacadeVoidInvocationDelegate BuildPipeline()
                {
                    if (index >= _interceptors.Length)
                        return target;
                    var currentIndex = index++;
                    var next = BuildPipeline();
                    if (_interceptors[currentIndex] is global::Terminus.ISyncVoidFacadeInterceptor<{{attributeTypeName}}> syncVoid)
                        return handlers => syncVoid.Intercept(context, nextHandlers => next(nextHandlers ?? handlers));
                    return next;
                }

                BuildPipeline()(null);
            }
            """)!;
    }

    /// <summary>
    /// Builds the synchronous result interceptor pipeline method for generic facades.
    /// </summary>
    public static MemberDeclarationSyntax BuildGenericSyncPipelineMethod(string attributeTypeName)
    {
        return ParseMemberDeclaration(
            $$"""
            private TResult ExecuteWithInterceptors<TResult>(global::Terminus.FacadeInvocationContext<{{attributeTypeName}}> context, global::Terminus.FacadeInvocationDelegate<TResult> target)
            {
                var index = 0;
                global::Terminus.FacadeInvocationDelegate<TResult> BuildPipeline()
                {
                    if (index >= _interceptors.Length)
                        return target;
                    var currentIndex = index++;
                    var next = BuildPipeline();
                    if (_interceptors[currentIndex] is global::Terminus.ISyncFacadeInterceptor<{{attributeTypeName}}> sync)
                        return handlers => sync.Intercept(context, nextHandlers => next(nextHandlers ?? handlers));
                    return next;
                }

                return BuildPipeline()(null);
            }
            """)!;
    }

    /// <summary>
    /// Builds the asynchronous void interceptor pipeline method for generic facades.
    /// </summary>
    public static MemberDeclarationSyntax BuildGenericAsyncVoidPipelineMethod(string attributeTypeName)
    {
        return ParseMemberDeclaration(
            $$"""
            private async global::System.Threading.Tasks.Task ExecuteWithAsyncVoidInterceptors(global::Terminus.FacadeInvocationContext<{{attributeTypeName}}> context, global::Terminus.FacadeAsyncVoidInvocationDelegate target)
            {
                var index = 0;
                global::Terminus.FacadeAsyncVoidInvocationDelegate BuildPipeline()
                {
                    if (index >= _interceptors.Length)
                        return target;
                    var currentIndex = index++;
                    var next = BuildPipeline();
                    if (_interceptors[currentIndex] is global::Terminus.IAsyncVoidFacadeInterceptor<{{attributeTypeName}}> asyncVoid)
                        return handlers => asyncVoid.InterceptAsync(context, nextHandlers => next(nextHandlers ?? handlers));
                    return next;
                }

                await BuildPipeline()(null).ConfigureAwait(false);
            }
            """)!;
    }

    /// <summary>
    /// Builds the asynchronous result interceptor pipeline method for generic facades.
    /// </summary>
    public static MemberDeclarationSyntax BuildGenericAsyncPipelineMethod(string attributeTypeName)
    {
        return ParseMemberDeclaration(
            $$"""
            private async global::System.Threading.Tasks.ValueTask<TResult> ExecuteWithInterceptorsAsync<TResult>(global::Terminus.FacadeInvocationContext<{{attributeTypeName}}> context, global::Terminus.FacadeAsyncInvocationDelegate<TResult> target)
            {
                var index = 0;
                global::Terminus.FacadeAsyncInvocationDelegate<TResult> BuildPipeline()
                {
                    if (index >= _interceptors.Length)
                        return target;
                    var currentIndex = index++;
                    var next = BuildPipeline();
                    if (_interceptors[currentIndex] is global::Terminus.IAsyncFacadeInterceptor<{{attributeTypeName}}> async)
                        return handlers => async.InterceptAsync(context, nextHandlers => next(nextHandlers ?? handlers));
                    return next;
                }

                return await BuildPipeline()(null).ConfigureAwait(false);
            }
            """)!;
    }

    /// <summary>
    /// Builds the streaming interceptor pipeline method for generic facades.
    /// </summary>
    public static MemberDeclarationSyntax BuildGenericStreamPipelineMethod(string attributeTypeName)
    {
        return ParseMemberDeclaration(
            $$"""
            private global::System.Collections.Generic.IAsyncEnumerable<TItem> ExecuteWithInterceptorsStream<TItem>(global::Terminus.FacadeInvocationContext<{{attributeTypeName}}> context, global::Terminus.FacadeStreamInvocationDelegate<TItem> target)
            {
                var index = 0;
                global::Terminus.FacadeStreamInvocationDelegate<TItem> BuildPipeline()
                {
                    if (index >= _interceptors.Length)
                        return target;
                    var currentIndex = index++;
                    var next = BuildPipeline();
                    if (_interceptors[currentIndex] is global::Terminus.IStreamFacadeInterceptor<{{attributeTypeName}}> stream)
                        return handlers => stream.InterceptStream(context, nextHandlers => next(nextHandlers ?? handlers));
                    return next;
                }

                return BuildPipeline()(null);
            }
            """)!;
    }
}
