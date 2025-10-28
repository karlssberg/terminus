using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Terminus.Generator.Tests.Unit.Generator.Infrastructure;

/// <summary>
/// Shared harness for Terminus source generator tests.
/// - Adds IsExternalInit shim so record types parse on older reference assemblies
/// - Adds minimal shims for Microsoft.Extensions.DependencyInjection to avoid heavy package references
/// - Adds a reference to Terminus so tests can use EndpointAttribute-derived types
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
        namespace Microsoft.Extensions.DependencyInjection
        {
            public interface IServiceCollection { }
            public static class ServiceCollectionExtensionsShim
            {
                public static IServiceCollection AddKeyedSingleton<T>(this IServiceCollection services, object? key, T implementation)
                    where T : class
                    => services;
            }
        }
        """;

    public TerminusSourceGeneratorTest()
    {
        // Common test inputs
        TestState.Sources.Add(IsExternalInitSource);
        TestState.Sources.Add(DiShimSource);
        
        // Ensure the test compilation can resolve Terminus.EntryPointAttribute
        TestState.AdditionalReferences.Add(
            MetadataReference.CreateFromFile(typeof(EntryPointAttribute).Assembly.Location));
    }
}
