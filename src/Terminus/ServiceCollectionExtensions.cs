using System;
using System.Linq;
using System.Reflection;
using Terminus;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Terminus facades with IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Scans the calling assembly for types decorated with <see cref="FacadeImplementationAttribute"/>
        /// and registers them with the service collection.
        /// Disposable facades (implementing <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/>)
        /// are registered as Scoped by default, while non-disposable facades are registered as Transient.
        /// </summary>
        /// <param name="lifetime">
        /// Optional explicit service lifetime. If not specified, uses Scoped for disposable facades
        /// and Transient for non-disposable facades.
        /// </param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// // Register all facades from calling assembly with default lifetimes
        /// services.AddTerminusFacades();
        /// 
        /// // Register all facades as singletons
        /// services.AddTerminusFacades(ServiceLifetime.Singleton);
        /// </code>
        /// </example>
        public IServiceCollection AddTerminusFacades(ServiceLifetime? lifetime = null)
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            return AddTerminusFacadesCore(services, lifetime, callingAssembly);
        }

        /// <summary>
        /// Scans the specified assemblies for types decorated with <see cref="FacadeImplementationAttribute"/>
        /// and registers them with the service collection.
        /// Disposable facades (implementing <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/>)
        /// are registered as Scoped by default, while non-disposable facades are registered as Transient.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// // Register facades from specific assemblies
        /// services.AddTerminusFacades(typeof(IMyFacade).Assembly, typeof(IOtherFacade).Assembly);
        /// </code>
        /// </example>
        public IServiceCollection AddTerminusFacades(params Assembly[] assemblies)
        {
            return AddTerminusFacadesCore(services, lifetime: null, assemblies);
        }

        /// <summary>
        /// Scans the specified assemblies for types decorated with <see cref="FacadeImplementationAttribute"/>
        /// and registers them with the service collection using the specified lifetime.
        /// </summary>
        /// <param name="lifetime">The service lifetime to use for all registered facades.</param>
        /// <param name="assemblies">The assemblies to scan.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// // Register facades from specific assembly as singletons
        /// services.AddTerminusFacades(ServiceLifetime.Singleton, typeof(IMyFacade).Assembly);
        /// </code>
        /// </example>
        public IServiceCollection AddTerminusFacades(ServiceLifetime lifetime,
            params Assembly[] assemblies)
        {
            return AddTerminusFacadesCore(services, lifetime, assemblies);
        }

        /// <summary>
        /// Registers a specific facade type from the specified assemblies.
        /// Disposable facades (implementing <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/>)
        /// are registered as Scoped by default, while non-disposable facades are registered as Transient.
        /// </summary>
        /// <typeparam name="TInterface">The facade interface type to register.</typeparam>
        /// <param name="assemblies">The assemblies to scan. If empty, scans the calling assembly.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// // Register from calling assembly
        /// services.AddTerminusFacade&lt;IMyFacade&gt;();
        ///
        /// // Register from specific assembly
        /// services.AddTerminusFacade&lt;IMyFacade&gt;(typeof(IMyFacade).Assembly);
        /// </code>
        /// </example>
        public IServiceCollection AddTerminusFacade<TInterface>(params Assembly[] assemblies)
            where TInterface : class
        {
            if (assemblies.Length == 0)
            {
                assemblies = [Assembly.GetCallingAssembly()];
            }
            return AddTerminusFacadeCore(services, typeof(TInterface), null, assemblies);
        }

        /// <summary>
        /// Registers a specific facade type from the specified assemblies with an explicit lifetime.
        /// </summary>
        /// <typeparam name="TInterface">The facade interface type to register.</typeparam>
        /// <param name="lifetime">The service lifetime to use.</param>
        /// <param name="assemblies">The assemblies to scan. If empty, scans the calling assembly.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddTerminusFacade&lt;IMyFacade&gt;(ServiceLifetime.Singleton);
        /// </code>
        /// </example>
        public IServiceCollection AddTerminusFacade<TInterface>(ServiceLifetime lifetime,
            params Assembly[] assemblies)
            where TInterface : class
        {
            if (assemblies.Length == 0)
            {
                assemblies = [Assembly.GetCallingAssembly()];
            }
            return AddTerminusFacadeCore(services, typeof(TInterface), lifetime, assemblies);
        }

        /// <summary>
        /// Registers a specific facade type from the specified assemblies.
        /// Disposable facades (implementing <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/>)
        /// are registered as Scoped by default, while non-disposable facades are registered as Transient.
        /// </summary>
        /// <param name="interfaceType">The facade interface type to register.</param>
        /// <param name="assemblies">The assemblies to scan. If empty, scans the calling assembly.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// // Register from calling assembly
        /// services.AddTerminusFacade(typeof(IMyFacade));
        ///
        /// // Register from specific assembly
        /// services.AddTerminusFacade(typeof(IMyFacade), typeof(IMyFacade).Assembly);
        /// </code>
        /// </example>
        public IServiceCollection AddTerminusFacade(
            Type interfaceType,
            params Assembly[] assemblies)
        {
            if (assemblies.Length == 0)
            {
                assemblies = [Assembly.GetCallingAssembly()];
            }
            return AddTerminusFacadeCore(services, interfaceType, null, assemblies);
        }

        /// <summary>
        /// Registers a specific facade type from the specified assemblies with an explicit lifetime.
        /// </summary>
        /// <param name="interfaceType">The facade interface type to register.</param>
        /// <param name="lifetime">The service lifetime to use.</param>
        /// <param name="assemblies">The assemblies to scan. If empty, scans the calling assembly.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddTerminusFacade(typeof(IMyFacade), ServiceLifetime.Singleton);
        /// </code>
        /// </example>
        public IServiceCollection AddTerminusFacade(
            Type interfaceType,
            ServiceLifetime lifetime,
            params Assembly[] assemblies)
        {
            if (assemblies.Length == 0)
            {
                assemblies = [Assembly.GetCallingAssembly()];
            }
            return AddTerminusFacadeCore(services, interfaceType, lifetime, assemblies);
        }
    }

    private static IServiceCollection AddTerminusFacadesCore(
        IServiceCollection services,
        ServiceLifetime? lifetime,
        params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var facadeImplementations = FindFacadeImplementations(assembly);

            foreach (var (implementationType, interfaceType) in facadeImplementations)
            {
                var effectiveLifetime = DetermineLifetime(implementationType, lifetime);

                services.Add(new ServiceDescriptor(
                    interfaceType,
                    implementationType,
                    effectiveLifetime));
            }
        }

        return services;
    }

    private static IServiceCollection AddTerminusFacadeCore(
        IServiceCollection services,
        Type interfaceType,
        ServiceLifetime? lifetime,
        params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var facadeImplementations = FindFacadeImplementations(assembly);

            var matchingImpl = facadeImplementations
                .FirstOrDefault(x => x.InterfaceType == interfaceType);

            if (matchingImpl != default)
            {
                var effectiveLifetime = DetermineLifetime(matchingImpl.ImplementationType, lifetime);

                services.Add(new ServiceDescriptor(
                    matchingImpl.InterfaceType,
                    matchingImpl.ImplementationType,
                    effectiveLifetime));

                return services;
            }
        }

        // No matching implementation found - silent no-op
        return services;
    }

    private static (Type ImplementationType, Type InterfaceType)[] FindFacadeImplementations(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<FacadeImplementationAttribute>() != null)
            .Select(type => (
                ImplementationType: type,
                InterfaceType: type.GetCustomAttribute<FacadeImplementationAttribute>()!.FacadeInterfaceType))
            .ToArray();
    }

    private static ServiceLifetime DetermineLifetime(Type implementationType, ServiceLifetime? requestedLifetime)
    {
        // If explicit lifetime provided, use it
        if (requestedLifetime.HasValue)
        {
            return requestedLifetime.Value;
        }

        // If implements IDisposable or IAsyncDisposable, use Scoped
        if (typeof(IDisposable).IsAssignableFrom(implementationType) ||
            typeof(IAsyncDisposable).IsAssignableFrom(implementationType))
        {
            return ServiceLifetime.Scoped;
        }

        // Default to Transient
        return ServiceLifetime.Transient;
    }
}
