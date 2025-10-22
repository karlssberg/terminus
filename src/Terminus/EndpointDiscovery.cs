using System.Reflection;

namespace Terminus;

/// <summary>
/// Provides functionality to discover endpoints in assemblies and types.
/// </summary>
public static class EndpointDiscovery
{
    /// <summary>
    /// Discovers all endpoints in the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for endpoints.</param>
    /// <returns>A collection of discovered endpoint metadata.</returns>
    public static IEnumerable<EndpointMetadata> DiscoverEndpoints(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var endpointTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEndpoint).IsAssignableFrom(t));

        foreach (var type in endpointTypes)
        {
            foreach (var metadata in DiscoverEndpoints(type))
            {
                yield return metadata;
            }
        }
    }

    /// <summary>
    /// Discovers all endpoints in the specified type.
    /// </summary>
    /// <param name="endpointType">The type to scan for endpoint methods.</param>
    /// <returns>A collection of discovered endpoint metadata.</returns>
    public static IEnumerable<EndpointMetadata> DiscoverEndpoints(Type endpointType)
    {
        if (endpointType == null)
            throw new ArgumentNullException(nameof(endpointType));

        if (!typeof(IEndpoint).IsAssignableFrom(endpointType))
            throw new ArgumentException($"Type {endpointType.Name} must implement IEndpoint", nameof(endpointType));

        var methods = endpointType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<EndpointAttribute>();
            if (attribute != null)
            {
                yield return new EndpointMetadata(endpointType, method, attribute);
            }
        }
    }

    /// <summary>
    /// Discovers all endpoints in the calling assembly.
    /// </summary>
    /// <returns>A collection of discovered endpoint metadata.</returns>
    public static IEnumerable<EndpointMetadata> DiscoverEndpoints()
    {
        return DiscoverEndpoints(Assembly.GetCallingAssembly());
    }
}
