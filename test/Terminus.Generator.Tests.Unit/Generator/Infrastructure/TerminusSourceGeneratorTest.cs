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
    private const string CreateAsyncScopeMethod =
        "public static AsyncServiceScope CreateAsyncScope(this IServiceProvider provider) => default;";

    private const string ServiceScopeClass =
        """
        public struct ServiceScope : IServiceScope, IDisposable
        {
            private readonly IServiceProvider _serviceProvider;
            public ServiceScope(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
            public IServiceProvider ServiceProvider => _serviceProvider;
            public void Dispose() { }
        }
        """;

    private const string AsyncServiceScopeClass =
        """
        public struct AsyncServiceScope : IAsyncDisposable
        {
            private readonly IServiceProvider _serviceProvider;
            public AsyncServiceScope(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
            public IServiceProvider ServiceProvider => _serviceProvider;
            public ValueTask DisposeAsync() => default;
        }
        """;

    private const string AsyncEnumerableShim = "";
#else
    private const string CreateAsyncScopeMethod = "";
    private const string ServiceScopeClass = "";
    private const string AsyncServiceScopeClass = "";

    private const string AsyncEnumerableShim =
        """
        namespace System.Threading.Tasks
        {
            public struct ValueTask
            {
                public bool IsCompleted => true;
                public void GetAwaiter() { }
            }

            public struct ValueTask<T>
            {
                private readonly T _value;
                public ValueTask(T value) { _value = value; }
                public bool IsCompleted => true;
                public T Result => _value;
                public System.Runtime.CompilerServices.ValueTaskAwaiter<T> GetAwaiter() => default;
            }
        }

        namespace System.Runtime.CompilerServices
        {
            public struct ValueTaskAwaiter<T>
            {
                public bool IsCompleted => true;
                public T GetResult() => default!;
                public void OnCompleted(System.Action continuation) { }
            }
        }
        """;
#endif

    private const string DiShimSource =
      $$"""
        using System;
        using System.Threading.Tasks;

        namespace Microsoft.Extensions.DependencyInjection
        {
            public interface IServiceCollection { }

            public class ServiceCollection : IServiceCollection { }

            public interface IServiceScope : IDisposable
            {
                IServiceProvider ServiceProvider { get; }
            }

            {{ServiceScopeClass}}
            {{AsyncServiceScopeClass}}

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
        
                public static IServiceCollection AddKeyedSingleton<TService>(this IServiceCollection services, object? key, Func<IServiceProvider, object?, TService> implementationFactory)
                    where TService : class
                    => services;
        
                public static IServiceCollection AddKeyedTransient<TService>(this IServiceCollection services, object? key, Func<IServiceProvider, object?, TService> implementationFactory)
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
                public static IServiceScope CreateScope(this IServiceProvider provider) => default!;
                {{CreateAsyncScopeMethod}}
                public static T GetRequiredService<T>(this IServiceProvider provider) => default!;
                public static T GetRequiredKeyedService<T>(this IServiceProvider provider, object? key) => default!;
            }
        }

        namespace System
        {
            public class Lazy<T>
            {
                private readonly Func<T> _valueFactory;
                private T _value;
                private bool _isValueCreated;

                public Lazy(Func<T> valueFactory)
                {
                    _valueFactory = valueFactory;
                }

                public bool IsValueCreated => _isValueCreated;

                public T Value
                {
                    get
                    {
                        if (!_isValueCreated)
                        {
                            _value = _valueFactory();
                            _isValueCreated = true;
                        }
                        return _value;
                    }
                }
            }
        }
        """;

    public TerminusSourceGeneratorTest()
    {
        // Align reference assemblies with the current test target framework
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        ReferenceAssemblies = ReferenceAssemblies.Net.Net80
            .AddPackages([new PackageIdentity("Microsoft.Bcl.AsyncInterfaces", "8.0.0")]);
#else
        ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
#endif

        // Add reference to Terminus assembly so tests can use FacadeOfAttribute
        TestState.AdditionalReferences.Add(typeof(Terminus.FacadeOfAttribute).Assembly);

        // Common test inputs
        TestState.Sources.Add(IsExternalInitSource);
        TestState.Sources.Add(DiShimSource);

        if (!string.IsNullOrEmpty(AsyncEnumerableShim))
        {
            TestState.Sources.Add(AsyncEnumerableShim);
        }
    }
}
