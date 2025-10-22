using Xunit;

namespace Terminus.Tests;

public class EndpointDiscoveryTests
{
    private class TestEndpoint : IEndpoint
    {
        [Endpoint]
        public void SimpleEndpoint()
        {
        }

        [Endpoint("CustomName")]
        public void NamedEndpoint()
        {
        }

        [Endpoint(Tags = new[] { "tag1", "tag2" })]
        public void TaggedEndpoint()
        {
        }

        public void NonEndpoint()
        {
        }
    }

    [Fact]
    public void DiscoverEndpoints_Type_FindsEndpointMethods()
    {
        // Act
        var endpoints = EndpointDiscovery.DiscoverEndpoints(typeof(TestEndpoint)).ToList();

        // Assert
        Assert.Equal(3, endpoints.Count);
        Assert.Contains(endpoints, e => e.Name == "SimpleEndpoint");
        Assert.Contains(endpoints, e => e.Name == "CustomName");
        Assert.Contains(endpoints, e => e.Name == "TaggedEndpoint");
    }

    [Fact]
    public void DiscoverEndpoints_Type_UsesCustomName()
    {
        // Act
        var endpoints = EndpointDiscovery.DiscoverEndpoints(typeof(TestEndpoint)).ToList();

        // Assert
        var namedEndpoint = endpoints.FirstOrDefault(e => e.Method.Name == "NamedEndpoint");
        Assert.NotNull(namedEndpoint);
        Assert.Equal("CustomName", namedEndpoint.Name);
    }

    [Fact]
    public void DiscoverEndpoints_Type_CapturesTags()
    {
        // Act
        var endpoints = EndpointDiscovery.DiscoverEndpoints(typeof(TestEndpoint)).ToList();

        // Assert
        var taggedEndpoint = endpoints.FirstOrDefault(e => e.Name == "TaggedEndpoint");
        Assert.NotNull(taggedEndpoint);
        Assert.Equal(2, taggedEndpoint.Tags.Count);
        Assert.Contains("tag1", taggedEndpoint.Tags);
        Assert.Contains("tag2", taggedEndpoint.Tags);
    }

    [Fact]
    public void DiscoverEndpoints_Type_ThrowsForNonEndpointType()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => EndpointDiscovery.DiscoverEndpoints(typeof(string)).ToList());
    }

    [Fact]
    public void DiscoverEndpoints_Type_ThrowsForNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EndpointDiscovery.DiscoverEndpoints((Type)null!).ToList());
    }

    [Fact]
    public void DiscoverEndpoints_Assembly_FindsEndpoints()
    {
        // Act
        var endpoints = EndpointDiscovery.DiscoverEndpoints(typeof(TestEndpoint).Assembly).ToList();

        // Assert
        Assert.True(endpoints.Count >= 3);
        Assert.Contains(endpoints, e => e.Name == "SimpleEndpoint");
    }
}
