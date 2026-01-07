using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Terminus.Generator.Tests.Unit.Generator.Infrastructure;

/// <summary>
/// Shared harness for Terminus source generator tests.
/// - Adds IsExternalInit shim so record types parse on older reference assemblies
/// - Adds minimal shims for Microsoft.Extensions.DependencyInjection to avoid heavy package references
/// - Adds a reference to Terminus so tests can use FacadeMethodAttribute-derived types
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

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    private const string DiShims =
        """
        namespace Microsoft.Extensions.DependencyInjection
        {
            using System;
            using System.Threading.Tasks;

            public interface IServiceScope : IDisposable
            {
                IServiceProvider ServiceProvider { get; }
            }

            public interface IServiceScopeFactory
            {
                IServiceScope CreateScope();
            }

            public struct ServiceScope : IServiceScope, IDisposable
            {
                private readonly IServiceProvider _serviceProvider;
                public ServiceScope(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
                public IServiceProvider ServiceProvider => _serviceProvider;
                public void Dispose() { }
            }

            public struct AsyncServiceScope : IAsyncDisposable
            {
                private readonly IServiceProvider _serviceProvider;
                public AsyncServiceScope(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
                public IServiceProvider ServiceProvider => _serviceProvider;
                public ValueTask DisposeAsync() => default;
            }

            public static class ServiceProviderServiceExtensions
            {
                public static T GetRequiredService<T>(this IServiceProvider provider) => default;
                public static IServiceScope CreateScope(this IServiceProvider provider) => default;
                public static AsyncServiceScope CreateAsyncScope(this IServiceProvider provider) => default;
            }
        }
        """;

    private const string AsyncEnumerableShim = "";
#else
    private const string DiShims =
        """
        namespace Microsoft.Extensions.DependencyInjection
        {
            using System;
            using System.Threading.Tasks;

            public interface IServiceScope : IDisposable
            {
                IServiceProvider ServiceProvider { get; }
            }

            public interface IServiceScopeFactory
            {
                IServiceScope CreateScope();
            }

            public struct ServiceScope : IServiceScope, IDisposable
            {
                private readonly IServiceProvider _serviceProvider;
                public ServiceScope(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
                public IServiceProvider ServiceProvider => _serviceProvider;
                public void Dispose() { }
            }

            public struct AsyncServiceScope : IAsyncDisposable
            {
                private readonly IServiceProvider _serviceProvider;
                public AsyncServiceScope(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
                public IServiceProvider ServiceProvider => _serviceProvider;
                public ValueTask DisposeAsync() => default;
            }

            public static class ServiceProviderServiceExtensions
            {
                public static T GetRequiredService<T>(this IServiceProvider provider) => default;
                public static IServiceScope CreateScope(this IServiceProvider provider) => default;
                public static AsyncServiceScope CreateAsyncScope(this IServiceProvider provider) => default;
            }
        }
        """;

    private const string AsyncEnumerableShim = "";
#endif

    public TerminusSourceGeneratorTest()
    {
        // Align reference assemblies with the current test target framework
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        ReferenceAssemblies = ReferenceAssemblies.Net.Net80
            .AddPackages([new PackageIdentity("Microsoft.Bcl.AsyncInterfaces", "8.0.0")]);
#else
        ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20
            .AddPackages([new PackageIdentity("Microsoft.Bcl.AsyncInterfaces", "1.1.1")]);
#endif
        // Add reference to Terminus assembly so tests can use FacadeOfAttribute
        TestState.AdditionalReferences.Add(typeof(Terminus.FacadeOfAttribute).Assembly);

        // Common test inputs
        TestState.Sources.Add(("IsExternalInit.cs", IsExternalInitSource));
        
        if (!string.IsNullOrEmpty(DiShims))
        {
            TestState.Sources.Add(("DiShims.cs", DiShims));
        }

        if (!string.IsNullOrEmpty(AsyncEnumerableShim))
        {
            TestState.Sources.Add(("AsyncEnumerableShim.cs", AsyncEnumerableShim));
        }
    }
}
