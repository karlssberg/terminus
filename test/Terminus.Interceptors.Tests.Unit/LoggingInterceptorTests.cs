using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Terminus.Interceptors.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="LoggingInterceptor"/>.
/// </summary>
public class LoggingInterceptorTests
{
    private readonly ILogger<LoggingInterceptor> _logger;
    private readonly LoggingInterceptor _sut;
    private readonly FacadeInvocationContext _context;

    public LoggingInterceptorTests()
    {
        _logger = Substitute.For<ILogger<LoggingInterceptor>>();
        _sut = new LoggingInterceptor(_logger);

        var serviceProvider = Substitute.For<IServiceProvider>();
        var method = typeof(ISampleFacade).GetMethod(nameof(ISampleFacade.DoWork))!;
        var attribute = new SampleAttribute();
        var properties = new Dictionary<string, object?>();

        var handlers = new FacadeVoidHandlerDescriptor[]
        {
            new(typeof(SampleHandler), attribute, isStatic: false, () => { })
        };

        _context = new FacadeInvocationContext(
            serviceProvider,
            method,
            new object?[] { "arg1", 42 },
            typeof(SampleHandler),
            attribute,
            properties,
            ReturnTypeKind.Void,
            handlers,
            isAggregated: false);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new LoggingInterceptor(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Intercept Tests (Synchronous)

    [Fact]
    public void Intercept_LogsInvocationStart()
    {
        // Arrange
        var wasCalled = false;

        // Act
        _sut.Intercept<object?>(_context, _ =>
        {
            wasCalled = true;
            return null;
        });

        // Assert
        wasCalled.Should().BeTrue();
        // Verify at least one Information log was made (start log)
        _logger.ReceivedWithAnyArgs().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Intercept_LogsInvocationCompletion()
    {
        // Act
        _sut.Intercept<object?>(_context, _ => null);

        // Assert - should log at least 2 messages (start and completion)
        _logger.ReceivedWithAnyArgs(2).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Intercept_ReturnsResultFromNext()
    {
        // Arrange
        const string expected = "test result";

        // Act
        var result = _sut.Intercept<string>(_context, _ => expected);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Intercept_OnException_LogsErrorAndRethrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var act = () => _sut.Intercept<object>(_context, _ => throw exception);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Test exception");
        _logger.ReceivedWithAnyArgs().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region InterceptAsync Tests

    [Fact]
    public async Task InterceptAsync_LogsInvocationStart()
    {
        // Arrange
        var wasCalled = false;

        // Act
        await _sut.InterceptAsync<object>(_context, _ =>
        {
            wasCalled = true;
            return new ValueTask<object?>(result: null);
        });

        // Assert
        wasCalled.Should().BeTrue();
        // Verify at least one Information log was made (start log)
        _logger.ReceivedWithAnyArgs().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task InterceptAsync_LogsInvocationCompletion()
    {
        // Act
        await _sut.InterceptAsync<object>(_context, _ => new ValueTask<object?>(result: null));

        // Assert - should log at least 2 messages (start and completion)
        _logger.ReceivedWithAnyArgs(2).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task InterceptAsync_ReturnsResultFromNext()
    {
        // Arrange
        const string expected = "async test result";

        // Act
        var result = await _sut.InterceptAsync<string>(_context, _ => new ValueTask<string?>(expected));

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task InterceptAsync_OnException_LogsErrorAndRethrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Async test exception");

        // Act
        var act = async () => await _sut.InterceptAsync<object>(_context, _ => throw exception);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Async test exception");
        _logger.ReceivedWithAnyArgs().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region InterceptStream Tests

    [Fact]
    public async Task InterceptStream_LogsInvocationStart()
    {
        // Arrange
        var wasCalled = false;

        // Act
        await foreach (var _ in _sut.InterceptStream<int>(_context, handlers =>
        {
            wasCalled = true;
            return GetEmptyStream();
        }))
        {
            // Consume the stream
        }

        // Assert
        wasCalled.Should().BeTrue();
        // Verify at least one Information log was made (start log)
        _logger.ReceivedWithAnyArgs().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task InterceptStream_YieldsAllItems()
    {
        // Arrange
        var expected = new[] { 1, 2, 3 };
        var items = new List<int>();

        // Act
        await foreach (var item in _sut.InterceptStream<int>(_context, _ => GetStream(expected)))
        {
            items.Add(item);
        }

        // Assert
        items.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task InterceptStream_LogsInvocationCompletion()
    {
        // Act
        await foreach (var _ in _sut.InterceptStream<int>(_context, _ => GetEmptyStream()))
        {
            // Consume the stream
        }

        // Assert - should log at least 2 messages (start and completion)
        _logger.ReceivedWithAnyArgs(2).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task InterceptStream_OnException_LogsErrorAndRethrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Stream test exception");

        // Act
        var act = async () =>
        {
            await foreach (var _ in _sut.InterceptStream<int>(_context, _ => GetThrowingStream(exception)))
            {
                // Consume the stream
            }
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Stream test exception");
        _logger.ReceivedWithAnyArgs().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region Helper Methods

    private static async IAsyncEnumerable<int> GetEmptyStream()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<int> GetStream(int[] items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    private static async IAsyncEnumerable<int> GetThrowingStream(Exception exception)
    {
        await Task.Yield();
        throw exception;
#pragma warning disable CS0162 // Unreachable code detected
        yield break;
#pragma warning restore CS0162
    }

    #endregion

    #region Test Types

    private interface ISampleFacade
    {
        void DoWork(string arg1, int arg2);
    }

    private class SampleHandler
    {
        public void DoWork(string arg1, int arg2) { }
    }

    private class SampleAttribute : Attribute { }

    #endregion
}
