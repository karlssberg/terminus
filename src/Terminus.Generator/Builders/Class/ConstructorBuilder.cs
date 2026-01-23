using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
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
              /// <summary>
              /// Initializes a new instance of the {{implementationClassName}} class.
              /// </summary>
              /// <param name="serviceProvider">The service provider used for resolving dependencies.</param>
              public {{implementationClassName}}(global::System.IServiceProvider serviceProvider)
              {
                  _serviceProvider = serviceProvider;
              }
              """)!;
    }

    /// <summary>
    /// Builds a constructor for non-scoped facades with interceptors.
    /// </summary>
    public static MemberDeclarationSyntax BuildNonScopedConstructorWithInterceptors(
        string implementationClassName,
        ImmutableArray<INamedTypeSymbol> interceptorTypes)
    {
        var interceptorInitialization = BuildInterceptorArrayInitialization(interceptorTypes);

        return ParseMemberDeclaration(
            $$"""
              /// <summary>
              /// Initializes a new instance of the {{implementationClassName}} class.
              /// </summary>
              /// <param name="serviceProvider">The service provider used for resolving dependencies.</param>
              public {{implementationClassName}}(global::System.IServiceProvider serviceProvider)
              {
                  _serviceProvider = serviceProvider;
                  _interceptors = new global::Terminus.IFacadeInterceptor[]
                  {
              {{interceptorInitialization}}
                  };
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
              /// <summary>
              /// Initializes a new instance of the {{implementationClassName}} class.
              /// </summary>
              /// <param name="serviceProvider">The service provider used for resolving dependencies.</param>
              public {{implementationClassName}}(global::System.IServiceProvider serviceProvider)
              {
                  _syncScope = new global::System.Lazy<global::Microsoft.Extensions.DependencyInjection.IServiceScope>(() => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>(serviceProvider).CreateScope());
                  _asyncScope = new global::System.Lazy<global::Microsoft.Extensions.DependencyInjection.AsyncServiceScope>(() => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateAsyncScope(serviceProvider));
              }
              """)!;
    }

    /// <summary>
    /// Builds a constructor for scoped facades with instance methods and interceptors.
    /// </summary>
    public static MemberDeclarationSyntax BuildScopedConstructorWithInterceptors(
        string implementationClassName,
        ImmutableArray<INamedTypeSymbol> interceptorTypes)
    {
        var interceptorInitialization = BuildInterceptorArrayInitialization(interceptorTypes);

        return ParseMemberDeclaration(
            $$"""
              /// <summary>
              /// Initializes a new instance of the {{implementationClassName}} class.
              /// </summary>
              /// <param name="serviceProvider">The service provider used for resolving dependencies.</param>
              public {{implementationClassName}}(global::System.IServiceProvider serviceProvider)
              {
                  _syncScope = new global::System.Lazy<global::Microsoft.Extensions.DependencyInjection.IServiceScope>(() => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>(serviceProvider).CreateScope());
                  _asyncScope = new global::System.Lazy<global::Microsoft.Extensions.DependencyInjection.AsyncServiceScope>(() => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateAsyncScope(serviceProvider));
                  _interceptors = new global::Terminus.IFacadeInterceptor[]
                  {
              {{interceptorInitialization}}
                  };
              }
              """)!;
    }

    private static string BuildInterceptorArrayInitialization(ImmutableArray<INamedTypeSymbol> interceptorTypes)
    {
        var lines = interceptorTypes.Select((type, index) =>
        {
            var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var suffix = index < interceptorTypes.Length - 1 ? "," : "";
            return $"        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{typeName}>(serviceProvider){suffix}";
        });

        return string.Join("\n", lines);
    }
}
