using System.Collections.Concurrent;
using System.Reflection;

namespace Terminus;

/// <summary>
/// A registry for managing discovered endpoints.
/// </summary>
public class EndpointRegistry
{
    private readonly ConcurrentDictionary<string, EndpointMetadata> _endpoints = new();

    /// <summary>
    /// Gets all registered endpoints.
    /// </summary>
    public IReadOnlyCollection<EndpointMetadata> Endpoints => _endpoints.Values.ToList();

    /// <summary>
    /// Registers an endpoint.
    /// </summary>
    /// <param name="metadata">The endpoint metadata to register.</param>
    /// <returns>True if the endpoint was registered; false if an endpoint with the same name already exists.</returns>
    public bool RegisterEndpoint(EndpointMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        return _endpoints.TryAdd(metadata.Name, metadata);
    }

    /// <summary>
    /// Registers multiple endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoints to register.</param>
    /// <returns>The number of endpoints successfully registered.</returns>
    public int RegisterEndpoints(IEnumerable<EndpointMetadata> endpoints)
    {
        if (endpoints == null)
            throw new ArgumentNullException(nameof(endpoints));

        int count = 0;
        foreach (var endpoint in endpoints)
        {
            if (RegisterEndpoint(endpoint))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Discovers and registers all endpoints in the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for endpoints.</param>
    /// <returns>The number of endpoints successfully registered.</returns>
    public int RegisterEndpoints(Assembly assembly)
    {
        var endpoints = EndpointDiscovery.DiscoverEndpoints(assembly);
        return RegisterEndpoints(endpoints);
    }

    /// <summary>
    /// Discovers and registers all endpoints in the specified type.
    /// </summary>
    /// <param name="endpointType">The type to scan for endpoint methods.</param>
    /// <returns>The number of endpoints successfully registered.</returns>
    public int RegisterEndpoints(Type endpointType)
    {
        var endpoints = EndpointDiscovery.DiscoverEndpoints(endpointType);
        return RegisterEndpoints(endpoints);
    }

    /// <summary>
    /// Attempts to get an endpoint by name.
    /// </summary>
    /// <param name="name">The name of the endpoint.</param>
    /// <param name="metadata">The endpoint metadata, if found.</param>
    /// <returns>True if the endpoint was found; otherwise, false.</returns>
    public bool TryGetEndpoint(string name, out EndpointMetadata? metadata)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Endpoint name cannot be null or empty", nameof(name));

        return _endpoints.TryGetValue(name, out metadata);
    }

    /// <summary>
    /// Gets an endpoint by name.
    /// </summary>
    /// <param name="name">The name of the endpoint.</param>
    /// <returns>The endpoint metadata.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no endpoint with the specified name is found.</exception>
    public EndpointMetadata GetEndpoint(string name)
    {
        if (TryGetEndpoint(name, out var metadata) && metadata != null)
            return metadata;

        throw new KeyNotFoundException($"No endpoint found with name '{name}'");
    }

    /// <summary>
    /// Clears all registered endpoints.
    /// </summary>
    public void Clear()
    {
        _endpoints.Clear();
    }

    /// <summary>
    /// Gets endpoints by tag.
    /// </summary>
    /// <param name="tag">The tag to filter by.</param>
    /// <returns>A collection of endpoints with the specified tag.</returns>
    public IEnumerable<EndpointMetadata> GetEndpointsByTag(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            throw new ArgumentException("Tag cannot be null or empty", nameof(tag));

        return _endpoints.Values.Where(e => e.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
    }
}
