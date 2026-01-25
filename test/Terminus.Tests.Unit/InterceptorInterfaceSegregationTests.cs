using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Terminus.Tests.Unit;

/// <summary>
/// Tests for the Interface Segregation Principle compliance of interceptor interfaces.
/// Verifies that interceptors implementing only specific interfaces are correctly invoked/skipped.
/// </summary>
public class InterceptorInterfaceSegregationTests
{
    private readonly FacadeInvocationContext _context;

    public InterceptorInterfaceSegregationTests()
    {
        var handlers = new FacadeHandlerDescriptor[]
        {
            new(typeof(SampleHandler), new SampleAttribute(), isStatic: false)
        };

        _context = new FacadeInvocationContext(
            serviceProvider: new StubServiceProvider(),
            method: typeof(ISampleFacade).GetMethod(nameof(ISampleFacade.DoWork))!,
            arguments: [],
            targetType: typeof(SampleHandler),
            methodAttribute: new SampleAttribute(),
            properties: new Dictionary<string, object?>(),
            returnTypeKind: ReturnTypeKind.Void,
            handlers: handlers,
            isAggregated: false);
    }

    #region ISyncFacadeInterceptor Tests

    [Fact]
    public void SyncOnlyInterceptor_ImplementsISyncFacadeInterceptor()
    {
        // Arrange
        var interceptor = new SyncOnlyInterceptor();

        // Assert
        Assert.IsAssignableFrom<ISyncFacadeInterceptor>(interceptor);
        Assert.False(interceptor is IAsyncFacadeInterceptor);
        Assert.False(interceptor is IStreamFacadeInterceptor);
    }

    [Fact]
    public void SyncOnlyInterceptor_IsInvokedForSyncMethods()
    {
        // Arrange
        var interceptor = new SyncOnlyInterceptor();
        var interceptors = new object[] { interceptor };
        var targetCalled = false;

        // Act - simulate the generated pipeline pattern
        var result = ExecuteWithInterceptors<string>(
            interceptors,
            _context,
            () =>
            {
                targetCalled = true;
                return "result";
            });

        // Assert
        Assert.True(interceptor.WasCalled, "sync-only interceptor should be called for sync methods");
        Assert.True(targetCalled, "target should be called");
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task SyncOnlyInterceptor_IsSkippedForAsyncMethods()
    {
        // Arrange
        var interceptor = new SyncOnlyInterceptor();
        var interceptors = new object[] { interceptor };
        var targetCalled = false;

        // Act - simulate the generated async pipeline pattern
        var result = await ExecuteWithInterceptorsAsync<string>(
            interceptors,
            _context,
            () =>
            {
                targetCalled = true;
                return new ValueTask<string?>("result");
            });

        // Assert
        Assert.False(interceptor.WasCalled, "sync-only interceptor should be skipped for async methods");
        Assert.True(targetCalled, "target should still be called");
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task SyncOnlyInterceptor_IsSkippedForStreamMethods()
    {
        // Arrange
        var interceptor = new SyncOnlyInterceptor();
        var interceptors = new object[] { interceptor };
        var items = new List<int>();

        // Act - simulate the generated stream pipeline pattern
        await foreach (var item in ExecuteWithInterceptorsStream<int>(
            interceptors,
            _context,
            () => GetStream(1, 2, 3)))
        {
            items.Add(item);
        }

        // Assert
        Assert.False(interceptor.WasCalled, "sync-only interceptor should be skipped for stream methods");
        Assert.Equal(new[] { 1, 2, 3 }, items);
    }

    #endregion

    #region IAsyncFacadeInterceptor Tests

    [Fact]
    public void AsyncOnlyInterceptor_ImplementsIAsyncFacadeInterceptor()
    {
        // Arrange
        var interceptor = new AsyncOnlyInterceptor();

        // Assert
        Assert.False(interceptor is ISyncFacadeInterceptor);
        Assert.IsAssignableFrom<IAsyncFacadeInterceptor>(interceptor);
        Assert.False(interceptor is IStreamFacadeInterceptor);
    }

    [Fact]
    public void AsyncOnlyInterceptor_IsSkippedForSyncMethods()
    {
        // Arrange
        var interceptor = new AsyncOnlyInterceptor();
        var interceptors = new object[] { interceptor };
        var targetCalled = false;

        // Act - simulate the generated pipeline pattern
        var result = ExecuteWithInterceptors<string>(
            interceptors,
            _context,
            () =>
            {
                targetCalled = true;
                return "result";
            });

        // Assert
        Assert.False(interceptor.WasCalled, "async-only interceptor should be skipped for sync methods");
        Assert.True(targetCalled, "target should still be called");
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task AsyncOnlyInterceptor_IsInvokedForAsyncMethods()
    {
        // Arrange
        var interceptor = new AsyncOnlyInterceptor();
        var interceptors = new object[] { interceptor };
        var targetCalled = false;

        // Act - simulate the generated async pipeline pattern
        var result = await ExecuteWithInterceptorsAsync<string>(
            interceptors,
            _context,
            () =>
            {
                targetCalled = true;
                return new ValueTask<string?>("result");
            });

        // Assert
        Assert.True(interceptor.WasCalled, "async-only interceptor should be called for async methods");
        Assert.True(targetCalled, "target should be called");
        Assert.Equal("result", result);
    }

    #endregion

    #region IStreamFacadeInterceptor Tests

    [Fact]
    public void StreamOnlyInterceptor_ImplementsIStreamFacadeInterceptor()
    {
        // Arrange
        var interceptor = new StreamOnlyInterceptor();

        // Assert
        Assert.False(interceptor is ISyncFacadeInterceptor);
        Assert.False(interceptor is IAsyncFacadeInterceptor);
        Assert.IsAssignableFrom<IStreamFacadeInterceptor>(interceptor);
    }

    [Fact]
    public void StreamOnlyInterceptor_IsSkippedForSyncMethods()
    {
        // Arrange
        var interceptor = new StreamOnlyInterceptor();
        var interceptors = new object[] { interceptor };
        var targetCalled = false;

        // Act
        var result = ExecuteWithInterceptors<string>(
            interceptors,
            _context,
            () =>
            {
                targetCalled = true;
                return "result";
            });

        // Assert
        Assert.False(interceptor.WasCalled, "stream-only interceptor should be skipped for sync methods");
        Assert.True(targetCalled, "target should still be called");
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task StreamOnlyInterceptor_IsInvokedForStreamMethods()
    {
        // Arrange
        var interceptor = new StreamOnlyInterceptor();
        var interceptors = new object[] { interceptor };
        var items = new List<int>();

        // Act
        await foreach (var item in ExecuteWithInterceptorsStream<int>(
            interceptors,
            _context,
            () => GetStream(1, 2, 3)))
        {
            items.Add(item);
        }

        // Assert
        Assert.True(interceptor.WasCalled, "stream-only interceptor should be called for stream methods");
        Assert.Equal(new[] { 1, 2, 3 }, items);
    }

    #endregion

    #region IFacadeInterceptor Tests (Full Interface)

    [Fact]
    public void FullInterceptor_ImplementsAllInterfaces()
    {
        // Arrange
        var interceptor = new FullInterceptor();

        // Assert
        Assert.IsAssignableFrom<ISyncFacadeInterceptor>(interceptor);
        Assert.IsAssignableFrom<IAsyncFacadeInterceptor>(interceptor);
        Assert.IsAssignableFrom<IStreamFacadeInterceptor>(interceptor);
        Assert.IsAssignableFrom<IFacadeInterceptor>(interceptor);
    }

    [Fact]
    public void FullInterceptor_IsInvokedForSyncMethods()
    {
        // Arrange
        var interceptor = new FullInterceptor();
        var interceptors = new object[] { interceptor };
        var targetCalled = false;

        // Act
        ExecuteWithInterceptors<string>(
            interceptors,
            _context,
            () =>
            {
                targetCalled = true;
                return "result";
            });

        // Assert
        Assert.True(interceptor.SyncCalled, "full interceptor should be called for sync methods");
        Assert.True(targetCalled);
    }

    [Fact]
    public async Task FullInterceptor_IsInvokedForAsyncMethods()
    {
        // Arrange
        var interceptor = new FullInterceptor();
        var interceptors = new object[] { interceptor };
        var targetCalled = false;

        // Act
        await ExecuteWithInterceptorsAsync<string>(
            interceptors,
            _context,
            () =>
            {
                targetCalled = true;
                return new ValueTask<string?>("result");
            });

        // Assert
        Assert.True(interceptor.AsyncCalled, "full interceptor should be called for async methods");
        Assert.True(targetCalled);
    }

    [Fact]
    public async Task FullInterceptor_IsInvokedForStreamMethods()
    {
        // Arrange
        var interceptor = new FullInterceptor();
        var interceptors = new object[] { interceptor };
        var items = new List<int>();

        // Act
        await foreach (var item in ExecuteWithInterceptorsStream<int>(
            interceptors,
            _context,
            () => GetStream(1, 2, 3)))
        {
            items.Add(item);
        }

        // Assert
        Assert.True(interceptor.StreamCalled, "full interceptor should be called for stream methods");
        Assert.Equal(new[] { 1, 2, 3 }, items);
    }

    #endregion

    #region Multiple Interceptors Tests

    [Fact]
    public void MixedInterceptors_OnlySyncInterceptorsInvokedForSyncMethods()
    {
        // Arrange
        var syncInterceptor = new SyncOnlyInterceptor();
        var asyncInterceptor = new AsyncOnlyInterceptor();
        var streamInterceptor = new StreamOnlyInterceptor();
        var fullInterceptor = new FullInterceptor();
        var interceptors = new object[] { syncInterceptor, asyncInterceptor, streamInterceptor, fullInterceptor };

        // Act
        ExecuteWithInterceptors<string>(
            interceptors,
            _context,
            () => "result");

        // Assert
        Assert.True(syncInterceptor.WasCalled, "sync-only interceptor should be called");
        Assert.False(asyncInterceptor.WasCalled, "async-only interceptor should be skipped");
        Assert.False(streamInterceptor.WasCalled, "stream-only interceptor should be skipped");
        Assert.True(fullInterceptor.SyncCalled, "full interceptor should be called");
    }

    [Fact]
    public async Task MixedInterceptors_OnlyAsyncInterceptorsInvokedForAsyncMethods()
    {
        // Arrange
        var syncInterceptor = new SyncOnlyInterceptor();
        var asyncInterceptor = new AsyncOnlyInterceptor();
        var streamInterceptor = new StreamOnlyInterceptor();
        var fullInterceptor = new FullInterceptor();
        var interceptors = new object[] { syncInterceptor, asyncInterceptor, streamInterceptor, fullInterceptor };

        // Act
        await ExecuteWithInterceptorsAsync<string>(
            interceptors,
            _context,
            () => new ValueTask<string?>("result"));

        // Assert
        Assert.False(syncInterceptor.WasCalled, "sync-only interceptor should be skipped");
        Assert.True(asyncInterceptor.WasCalled, "async-only interceptor should be called");
        Assert.False(streamInterceptor.WasCalled, "stream-only interceptor should be skipped");
        Assert.True(fullInterceptor.AsyncCalled, "full interceptor should be called");
    }

    [Fact]
    public async Task MixedInterceptors_OnlyStreamInterceptorsInvokedForStreamMethods()
    {
        // Arrange
        var syncInterceptor = new SyncOnlyInterceptor();
        var asyncInterceptor = new AsyncOnlyInterceptor();
        var streamInterceptor = new StreamOnlyInterceptor();
        var fullInterceptor = new FullInterceptor();
        var interceptors = new object[] { syncInterceptor, asyncInterceptor, streamInterceptor, fullInterceptor };
        var items = new List<int>();

        // Act
        await foreach (var item in ExecuteWithInterceptorsStream<int>(
            interceptors,
            _context,
            () => GetStream(1, 2, 3)))
        {
            items.Add(item);
        }

        // Assert
        Assert.False(syncInterceptor.WasCalled, "sync-only interceptor should be skipped");
        Assert.False(asyncInterceptor.WasCalled, "async-only interceptor should be skipped");
        Assert.True(streamInterceptor.WasCalled, "stream-only interceptor should be called");
        Assert.True(fullInterceptor.StreamCalled, "full interceptor should be called");
    }

    #endregion

    #region Pipeline Methods (Simulating Generated Code)

    /// <summary>
    /// Simulates the generated ExecuteWithInterceptors method with ISP-compliant interface checks.
    /// </summary>
    private static TResult? ExecuteWithInterceptors<TResult>(
        object[] interceptors,
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> target)
    {
        var index = 0;

        FacadeInvocationDelegate<TResult> BuildPipeline()
        {
            if (index >= interceptors.Length)
                return target;

            var currentIndex = index++;
            var next = BuildPipeline();

            if (interceptors[currentIndex] is ISyncFacadeInterceptor sync)
                return () => sync.Intercept(context, next);

            return next;
        }

        return BuildPipeline()();
    }

    /// <summary>
    /// Simulates the generated ExecuteWithInterceptorsAsync method with ISP-compliant interface checks.
    /// </summary>
    private static async ValueTask<TResult?> ExecuteWithInterceptorsAsync<TResult>(
        object[] interceptors,
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> target)
    {
        var index = 0;

        FacadeAsyncInvocationDelegate<TResult> BuildPipeline()
        {
            if (index >= interceptors.Length)
                return target;

            var currentIndex = index++;
            var next = BuildPipeline();

            if (interceptors[currentIndex] is IAsyncFacadeInterceptor async)
                return () => async.InterceptAsync(context, next);

            return next;
        }

        return await BuildPipeline()().ConfigureAwait(false);
    }

    /// <summary>
    /// Simulates the generated ExecuteWithInterceptorsStream method with ISP-compliant interface checks.
    /// </summary>
    private static IAsyncEnumerable<TItem> ExecuteWithInterceptorsStream<TItem>(
        object[] interceptors,
        FacadeInvocationContext context,
        FacadeStreamInvocationDelegate<TItem> target)
    {
        var index = 0;

        FacadeStreamInvocationDelegate<TItem> BuildPipeline()
        {
            if (index >= interceptors.Length)
                return target;

            var currentIndex = index++;
            var next = BuildPipeline();

            if (interceptors[currentIndex] is IStreamFacadeInterceptor stream)
                return () => stream.InterceptStream(context, next);

            return next;
        }

        return BuildPipeline()();
    }

    #endregion

    #region Helper Methods

    private static async IAsyncEnumerable<int> GetStream(params int[] items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    #endregion

    #region Test Types

    private interface ISampleFacade
    {
        void DoWork();
    }

    private class SampleHandler
    {
        public void DoWork() { }
    }

    private class SampleAttribute : Attribute { }

    /// <summary>
    /// Stub service provider for testing purposes.
    /// </summary>
    private class StubServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    /// <summary>
    /// Interceptor that only implements ISyncFacadeInterceptor.
    /// </summary>
    private class SyncOnlyInterceptor : ISyncFacadeInterceptor
    {
        public bool WasCalled { get; private set; }

        public TResult? Intercept<TResult>(FacadeInvocationContext context, FacadeInvocationDelegate<TResult> next)
        {
            WasCalled = true;
            return next();
        }
    }

    /// <summary>
    /// Interceptor that only implements IAsyncFacadeInterceptor.
    /// </summary>
    private class AsyncOnlyInterceptor : IAsyncFacadeInterceptor
    {
        public bool WasCalled { get; private set; }

        public ValueTask<TResult?> InterceptAsync<TResult>(FacadeInvocationContext context, FacadeAsyncInvocationDelegate<TResult> next)
        {
            WasCalled = true;
            return next();
        }
    }

    /// <summary>
    /// Interceptor that only implements IStreamFacadeInterceptor.
    /// </summary>
    private class StreamOnlyInterceptor : IStreamFacadeInterceptor
    {
        public bool WasCalled { get; private set; }

        public async IAsyncEnumerable<TItem> InterceptStream<TItem>(FacadeInvocationContext context, FacadeStreamInvocationDelegate<TItem> next)
        {
            WasCalled = true;
            await foreach (var item in next())
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Interceptor that implements the full IFacadeInterceptor interface (all three).
    /// </summary>
    private class FullInterceptor : IFacadeInterceptor
    {
        public bool SyncCalled { get; private set; }
        public bool AsyncCalled { get; private set; }
        public bool StreamCalled { get; private set; }

        public TResult? Intercept<TResult>(FacadeInvocationContext context, FacadeInvocationDelegate<TResult> next)
        {
            SyncCalled = true;
            return next();
        }

        public ValueTask<TResult?> InterceptAsync<TResult>(FacadeInvocationContext context, FacadeAsyncInvocationDelegate<TResult> next)
        {
            AsyncCalled = true;
            return next();
        }

        public async IAsyncEnumerable<TItem> InterceptStream<TItem>(FacadeInvocationContext context, FacadeStreamInvocationDelegate<TItem> next)
        {
            StreamCalled = true;
            await foreach (var item in next())
            {
                yield return item;
            }
        }
    }

    #endregion
}
