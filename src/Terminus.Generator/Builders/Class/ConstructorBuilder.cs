using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Class;

/// <summary>
/// Builds constructor declarations for facade implementation classes.
/// </summary>
internal static class ConstructorBuilder
{
    /// <summary>
    /// Builds a constructor for non-scoped facades.
    /// </summary>
    public static MemberDeclarationSyntax BuildNonScopedConstructor(string implementationClassName)
    {
        return ParseMemberDeclaration(
            $$"""
              public {{implementationClassName}}(global::System.IServiceProvider serviceProvider)
              {
                  _serviceProvider = serviceProvider;
              }
              """)!;
    }

    /// <summary>
    /// Builds a constructor for scoped facades with instance methods.
    /// </summary>
    public static MemberDeclarationSyntax BuildScopedConstructor(string implementationClassName)
    {
        return ParseMemberDeclaration(
            $$"""
              public {{implementationClassName}}(global::System.IServiceProvider serviceProvider)
              {
                  _syncScope = new global::System.Lazy<global::Microsoft.Extensions.DependencyInjection.IServiceScope>(() => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>(serviceProvider).CreateScope());
                  _asyncScope = new global::System.Lazy<global::Microsoft.Extensions.DependencyInjection.AsyncServiceScope>(() => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateAsyncScope(serviceProvider));
              }
              """)!;
    }
}
