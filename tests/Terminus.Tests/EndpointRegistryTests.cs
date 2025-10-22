using Xunit;

namespace Terminus.Tests;

public class EndpointRegistryTests
{
    private class TestEndpoint : IEndpoint
    {
        [Endpoint]
        public void Endpoint1()
        {
        }

        [Endpoint]
        public void Endpoint2()
        {
        }

        [Endpoint(Tags = new[] { "test" })]
        public void TaggedEndpoint()
        {
        }
    }

    [Fact]
    public void RegisterEndpoint_AddsEndpoint()
    {
        // Arrange
        var registry = new EndpointRegistry();
        var endpoints = EndpointDiscovery.DiscoverEndpoints(typeof(TestEndpoint)).ToList();

        // Act
        var result = registry.RegisterEndpoint(endpoints[0]);

        // Assert
        Assert.True(result);
        Assert.Single(registry.Endpoints);
    }

    [Fact]
    public void RegisterEndpoint_ReturnsFalseForDuplicate()
    {
        // Arrange
        var registry = new EndpointRegistry();
        var endpoints = EndpointDiscovery.DiscoverEndpoints(typeof(TestEndpoint)).ToList();

        // Act
        registry.RegisterEndpoint(endpoints[0]);
        var result = registry.RegisterEndpoint(endpoints[0]);

        // Assert
        Assert.False(result);
        Assert.Single(registry.Endpoints);
    }

    [Fact]
    public void RegisterEndpoints_Type_AddsAllEndpoints()
    {
        // Arrange
        var registry = new EndpointRegistry();

        // Act
        var count = registry.RegisterEndpoints(typeof(TestEndpoint));

        // Assert
        Assert.Equal(3, count);
        Assert.Equal(3, registry.Endpoints.Count);
    }

    [Fact]
    public void TryGetEndpoint_FindsEndpoint()
    {
        // Arrange
        var registry = new EndpointRegistry();
        registry.RegisterEndpoints(typeof(TestEndpoint));

        // Act
        var result = registry.TryGetEndpoint("Endpoint1", out var metadata);

        // Assert
        Assert.True(result);
        Assert.NotNull(metadata);
        Assert.Equal("Endpoint1", metadata.Name);
    }

    [Fact]
    public void TryGetEndpoint_ReturnsFalseForMissing()
    {
        // Arrange
        var registry = new EndpointRegistry();

        // Act
        var result = registry.TryGetEndpoint("NonExistent", out var metadata);

        // Assert
        Assert.False(result);
        Assert.Null(metadata);
    }

    [Fact]
    public void GetEndpoint_ReturnsEndpoint()
    {
        // Arrange
        var registry = new EndpointRegistry();
        registry.RegisterEndpoints(typeof(TestEndpoint));

        // Act
        var metadata = registry.GetEndpoint("Endpoint1");

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Endpoint1", metadata.Name);
    }

    [Fact]
    public void GetEndpoint_ThrowsForMissing()
    {
        // Arrange
        var registry = new EndpointRegistry();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => registry.GetEndpoint("NonExistent"));
    }

    [Fact]
    public void Clear_RemovesAllEndpoints()
    {
        // Arrange
        var registry = new EndpointRegistry();
        registry.RegisterEndpoints(typeof(TestEndpoint));

        // Act
        registry.Clear();

        // Assert
        Assert.Empty(registry.Endpoints);
    }

    [Fact]
    public void GetEndpointsByTag_FiltersCorrectly()
    {
        // Arrange
        var registry = new EndpointRegistry();
        registry.RegisterEndpoints(typeof(TestEndpoint));

        // Act
        var endpoints = registry.GetEndpointsByTag("test").ToList();

        // Assert
        Assert.Single(endpoints);
        Assert.Equal("TaggedEndpoint", endpoints[0].Name);
    }

    [Fact]
    public void GetEndpointsByTag_IsCaseInsensitive()
    {
        // Arrange
        var registry = new EndpointRegistry();
        registry.RegisterEndpoints(typeof(TestEndpoint));

        // Act
        var endpoints = registry.GetEndpointsByTag("TEST").ToList();

        // Assert
        Assert.Single(endpoints);
        Assert.Equal("TaggedEndpoint", endpoints[0].Name);
    }
}
