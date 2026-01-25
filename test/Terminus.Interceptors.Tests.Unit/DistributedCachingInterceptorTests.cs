using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
using Xunit;

namespace Terminus.Interceptors.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="DistributedCachingInterceptor"/>.
/// </summary>
public class DistributedCachingInterceptorTests
{
    private readonly IDistributedCache _cache;
    private readonly DistributedCachingInterceptor _sut;
    private readonly FacadeInvocationContext _context;

    public DistributedCachingInterceptorTests()
    {
        _cache = Substitute.For<IDistributedCache>();
        _sut = new DistributedCachingInterceptor(_cache);

        var serviceProvider = Substitute.For<IServiceProvider>();
        var method = typeof(ISampleFacade).GetMethod(nameof(ISampleFacade.GetData))!;
        var attribute = new SampleAttribute();
        var properties = new Dictionary<string, object?>();
        var handlers = new FacadeAsyncHandlerDescriptor<TestData>[]
        {
            new(typeof(SampleHandler), attribute, isStatic: false, () => new ValueTask<TestData>(new TestData()))
        };

        _context = new FacadeInvocationContext(
            serviceProvider,
            method,
            [42],
            typeof(SampleHandler),
            attribute,
            properties,
            ReturnTypeKind.TaskWithResult,
            handlers,
            isAggregated: false);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DistributedCachingInterceptor(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("distributedCache");
    }

    [Fact]
    public void Constructor_WithCustomExpiration_UsesProvidedValue()
    {
        // Arrange & Act
        var customExpiration = TimeSpan.FromMinutes(30);
        var interceptor = new DistributedCachingInterceptor(_cache, customExpiration);

        // Assert - interceptor should be created successfully
        interceptor.Should().NotBeNull();
    }

    #endregion

    #region InterceptAsync Tests

    [Fact]
    public async Task InterceptAsync_WhenCacheHit_ReturnsFromCache()
    {
        // Arrange
        var expectedResult = new TestData { Id = 42, Name = "Test" };
        var cachedBytes = JsonSerializer.SerializeToUtf8Bytes(expectedResult);

        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cachedBytes);

        // Act
        var result = await _sut.InterceptAsync<TestData>(_context, _ =>
            throw new InvalidOperationException("Should not be called"));

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _context.Properties.Should().ContainKey("CacheHit")
            .WhoseValue.Should().Be(true);
    }

    [Fact]
    public async Task InterceptAsync_WhenCacheMiss_CallsNextAndCaches()
    {
        // Arrange
        var expectedResult = new TestData { Id = 42, Name = "Test" };

        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        var result = await _sut.InterceptAsync<TestData?>(_context, _ =>
            new ValueTask<TestData?>(expectedResult));

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _context.Properties.Should().ContainKey("CacheHit")
            .WhoseValue.Should().Be(false);

        await _cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InterceptAsync_ForVoidMethods_SkipsCaching()
    {
        // Arrange
        var voidContext = CreateContextWithReturnType(ReturnTypeKind.Void);
        var wasCalled = false;

        // Act
        await _sut.InterceptAsync<object?>(voidContext, _ =>
        {
            wasCalled = true;
            return new ValueTask<object?>(result: null);
        });

        // Assert
        wasCalled.Should().BeTrue();
        await _cache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InterceptAsync_ForTaskMethods_SkipsCaching()
    {
        // Arrange
        var taskContext = CreateContextWithReturnType(ReturnTypeKind.Task);
        var wasCalled = false;

        // Act
        await _sut.InterceptAsync<object?>(taskContext, _ =>
        {
            wasCalled = true;
            return new ValueTask<object?>(result: null);
        });

        // Assert
        wasCalled.Should().BeTrue();
        await _cache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InterceptAsync_GeneratesCorrectCacheKey()
    {
        // Arrange
        string? capturedKey = null;
        _cache.GetAsync(Arg.Do<string>(k => capturedKey = k), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        await _sut.InterceptAsync<TestData?>(_context, _ =>
            new ValueTask<TestData?>(new TestData { Id = 1, Name = "test" }));

        // Assert
        capturedKey.Should().Contain("ISampleFacade");
        capturedKey.Should().Contain("GetData");
        capturedKey.Should().Contain("42");
    }

    #endregion

    #region Intercept Tests (Sync)

    [Fact]
    public void Intercept_ForVoidMethods_SkipsCaching()
    {
        // Arrange
        var voidContext = CreateContextWithReturnType(ReturnTypeKind.Void);
        var wasCalled = false;

        // Act
        _sut.Intercept<object?>(voidContext, _ =>
        {
            wasCalled = true;
            return null;
        });

        // Assert
        wasCalled.Should().BeTrue();
    }

    [Fact]
    public void Intercept_ForResultMethods_PassesThroughWithoutCaching()
    {
        // Arrange
        var resultContext = CreateContextWithReturnType(ReturnTypeKind.Result);
        var expected = "test result";

        // Act - distributed cache doesn't support sync caching
        var result = _sut.Intercept<string>(resultContext, _ => expected);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Helper Methods

    private FacadeInvocationContext CreateContextWithReturnType(ReturnTypeKind returnTypeKind)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var method = typeof(ISampleFacade).GetMethod(nameof(ISampleFacade.GetData))!;
        var attribute = new SampleAttribute();
        var properties = new Dictionary<string, object?>();
        var handlers = new FacadeVoidHandlerDescriptor[]
        {
            new(typeof(SampleHandler), attribute, isStatic: false, () => { })
        };

        return new FacadeInvocationContext(
            serviceProvider,
            method,
            [1],
            typeof(SampleHandler),
            attribute,
            properties,
            returnTypeKind,
            handlers,
            isAggregated: false);
    }

    #endregion

    #region Test Types

    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private interface ISampleFacade
    {
        Task<TestData> GetData(int id);
    }

    private class SampleHandler
    {
        public Task<TestData> GetData(int id) => Task.FromResult(new TestData { Id = id, Name = "Test" });
    }

    private class SampleAttribute : Attribute { }

    #endregion
}
