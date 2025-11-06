using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Terminus.Generator.Tests.Unit.Generator.Infrastructure;

/// <summary>
/// Shared harness for Terminus source generator tests.
/// - Adds IsExternalInit shim so record types parse on older reference assemblies
/// - Adds minimal shims for Microsoft.Extensions.DependencyInjection to avoid heavy package references
/// - Adds a reference to Terminus so tests can use EntryPointAttribute-derived types
/// </summary>
/// <typeparam name="TGenerator">The generator under test.</typeparam>
public class TerminusSourceGeneratorTest<TGenerator> : CSharpSourceGeneratorTest<TGenerator, DefaultVerifier>
    where TGenerator : new()
{
    private const string IsExternalInitSource =
        """
        namespace System.Runtime.CompilerServices
        {
            internal static class IsExternalInit { }
        }
        """;

    private const string DiShimSource =
        """
        using System;
        namespace Microsoft.Extensions.DependencyInjection
        {
            public interface IServiceCollection { }
            public interface IServiceScope : IDisposable
            {
                IServiceProvider ServiceProvider { get; }
            }
            public static class ServiceCollectionExtensionsShim
            {
                public static IServiceCollection AddSingleton<T>(this IServiceCollection services, T implementation)
                    where T : class
                    => services;
                
                public static IServiceCollection AddSingleton<TService, TImplementation>(this IServiceCollection services)
                    where TService : class
                    where TImplementation : class, TService
                    => services;
                
                public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
                    where TService : class
                    => services;
                
                public static IServiceCollection AddTransient<TService, TImplementation>(this IServiceCollection services)
                    where TService : class
                    where TImplementation : class, TService
                    => services;
                    
                public static IServiceCollection AddTransient<TService>(this IServiceCollection services)
                    where TService : class
                    => services;
            }
            
            public static class ServiceProviderExtensionsShim
            {
                public static IServiceScope CreateScope(this IServiceProvider provider) => null!;
                public static T GetRequiredService<T>(this IServiceProvider provider) => default!;
            }
        }
        """;

    public TerminusSourceGeneratorTest()
    {
        // Common test inputs
        TestState.Sources.Add(IsExternalInitSource);
        TestState.Sources.Add(DiShimSource);
        
        // Ensure the test compilation can resolve Terminus.EntryPointAttribute and other Terminus types
        TestState.AdditionalReferences.Add(
            MetadataReference.CreateFromFile(typeof(EntryPointAttribute).Assembly.Location));
        TestState.AdditionalReferences.Add(
            MetadataReference.CreateFromFile(typeof(Terminus.EntryPointDescriptor<>).Assembly.Location));
    }
}
