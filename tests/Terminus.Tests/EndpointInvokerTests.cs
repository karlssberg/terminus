using Xunit;

namespace Terminus.Tests;

public class EndpointInvokerTests
{
    private class TestEndpoint : IEndpoint
    {
        public int InvocationCount { get; private set; }
        public string? LastMessage { get; private set; }

        [Endpoint]
        public string Echo(string message)
        {
            InvocationCount++;
            LastMessage = message;
            return message;
        }

        [Endpoint]
        public int Add(int a, int b)
        {
            InvocationCount++;
            return a + b;
        }

        [Endpoint]
        public void VoidMethod()
        {
            InvocationCount++;
        }

        [Endpoint]
        public async Task<string> AsyncMethod(string message)
        {
            InvocationCount++;
            await Task.Delay(1);
            return message;
        }

        [Endpoint]
        public async Task VoidAsyncMethod()
        {
            InvocationCount++;
            await Task.Delay(1);
        }
    }

    [Fact]
    public void Invoke_CallsMethod()
    {
        // Arrange
        var endpoint = new TestEndpoint();
        var metadata = EndpointDiscovery.DiscoverEndpoints(typeof(TestEndpoint))
            .First(e => e.Name == "Echo");

        // Act
        var result = EndpointInvoker.Invoke(metadata, endpoint, "test");

        // Assert
        Assert.Equal("test", result);
        Assert.Equal(1, endpoint.InvocationCount);
        Assert.Equal("test", endpoint.LastMessage);
    }

    [Fact]
    public void Invoke_WithMultipleParameters()
    {
        // Arrange
        var endpoint = new TestEndpoint();
        var metadata = EndpointDiscovery.DiscoverEndpoints(typeof(TestEndpoint))
            .First(e => e.Name == "Add");

        // Act
        var result = EndpointInvoker.Invoke(metadata, endpoint, 5, 3);

        // Assert
        Assert.Equal(8, result);
        Assert.Equal(1, endpoint.InvocationCount);
    }

    [Fact]
    public void Invoke_VoidMethod_ReturnsNull()
    {
        // Arrange
        var endpoint = new TestEndpoint();
        var metadata = EndpointDiscovery.DiscoverEndpoints(typeof(TestEndpoint))
            .First(e => e.Name == "VoidMethod");

        // Act
        var result = EndpointInvoker.Invoke(metadata, endpoint);

        // Assert
        Assert.Null(result);
        Assert.Equal(1, endpoint.InvocationCount);
    }

    [Fact]
    public async Task InvokeAsync_CallsAsyncMethod()
    {
        // Arrange
        var endpoint = new TestEndpoint();
        var metadata = EndpointDiscovery.DiscoverEndpoints(typeof(TestEndpoint))
            .First(e => e.Name == "AsyncMethod");

        // Act
        var result = await EndpointInvoker.InvokeAsync(metadata, endpoint, "async test");

        // Assert
        Assert.Equal("async test", result);
        Assert.Equal(1, endpoint.InvocationCount);
    }

    [Fact]
    public async Task InvokeAsync_VoidAsyncMethod_Completes()
    {
        // Arrange
        var endpoint = new TestEndpoint();
        var metadata = EndpointDiscovery.DiscoverEndpoints(typeof(TestEndpoint))
            .First(e => e.Name == "VoidAsyncMethod");

        // Act
        await EndpointInvoker.InvokeAsync(metadata, endpoint);

        // Assert
        Assert.Equal(1, endpoint.InvocationCount);
    }

    [Fact]
    public async Task InvokeAsync_SyncMethod_Works()
    {
        // Arrange
        var endpoint = new TestEndpoint();
        var metadata = EndpointDiscovery.DiscoverEndpoints(typeof(TestEndpoint))
            .First(e => e.Name == "Echo");

        // Act
        var result = await EndpointInvoker.InvokeAsync(metadata, endpoint, "sync via async");

        // Assert
        Assert.Equal("sync via async", result);
        Assert.Equal(1, endpoint.InvocationCount);
    }

    [Fact]
    public void Invoke_ThrowsForWrongInstanceType()
    {
        // Arrange
        var wrongInstance = new object();
        var metadata = EndpointDiscovery.DiscoverEndpoints(typeof(TestEndpoint))
            .First(e => e.Name == "Echo");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => EndpointInvoker.Invoke(metadata, wrongInstance, "test"));
    }
}
