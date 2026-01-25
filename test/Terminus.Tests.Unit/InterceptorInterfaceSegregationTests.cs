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
            new FacadeVoidHandlerDescriptor(typeof(SampleHandler), new SampleAttribute(), isStatic: false, () => { })
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

    #region ISyncFacadeInterceptor Tests (Result Methods)

    [Fact]
    public void SyncResultOnlyInterceptor_ImplementsISyncFacadeInterceptor()
    {
        // Arrange
        var interceptor = new SyncResultOnlyInterceptor();

        // Assert
        Assert.IsAssignableFrom<ISyncFacadeInterceptor>(interceptor);
        Assert.False(interceptor is ISyncVoidFacadeInterceptor);
        Assert.False(interceptor is IAsyncFacadeInterceptor);
        Assert.False(interceptor is IStreamFacadeInterceptor);
    }

    [Fact]
    public void SyncResultOnlyInterceptor_IsInvokedForSyncResultMethods()
    {
        // Arrange
        var interceptor = new SyncResultOnlyInterceptor();
        var interceptors = new object[] { interceptor };
        var targetCalled = false;

        // Act - simulate the generated pipeline pattern
        var result = ExecuteWithInterceptors<string>(
            interceptors,
            _context,
            _ =>
            {
                targetCalled = true;
                return "result";
            });

        // Assert
        Assert.True(interceptor.WasCalled, "sync result interceptor should be called for sync result methods");
        Assert.True(targetCalled, "target should be called");
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task SyncResultOnlyInterceptor_IsSkippedForAsyncMethods()
    {
        // Arrange
        var interceptor = new SyncResultOnlyInterceptor();
        var interceptors = new object[] { interceptor };
        var targetCalled = false;

        // Act - simulate the generated async pipeline pattern
        var result = await ExecuteWithInterceptorsAsync<string>(
            interceptors,
            _context,
            _ =>
            {
                targetCalled = true;
                return new ValueTask<string>("result");
            });

        // Assert
        Assert.False(interceptor.WasCalled, "sync result interceptor should be skipped for async methods");
        Assert.True(targetCalled, "target should still be called");
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task SyncResultOnlyInterceptor_IsSkippedForStreamMethods()
    {
        // Arrange
        var interceptor = new SyncResultOnlyInterceptor();
        var interceptors = new object[] { interceptor };
        var items = new List<int>();

        // Act - simulate the generated stream pipeline pattern
        await foreach (var item in ExecuteWithInterceptorsStream<int>(
            interceptors,
            _context,
            _ => GetStream(1, 2, 3)))
        {
            items.Add(item);
        }

        // Assert
        Assert.False(interceptor.WasCalled, "sync result interceptor should be skipped for stream methods");
        Assert.Equal(new[] { 1, 2, 3 }, items);
    }

    #endregion

    #region ISyncVoidFacadeInterceptor Tests

    [Fact]
    public void SyncVoidOnlyInterceptor_ImplementsISyncVoidFacadeInterceptor()
    {
        // Arrange
        var interceptor = new SyncVoidOnlyInterceptor();

        // Assert
        Assert.IsAssignableFrom<ISyncVoidFacadeInterceptor>(interceptor);
        Assert.False(interceptor is ISyncFacadeInterceptor);
        Assert.False(interceptor is IAsyncFacadeInterceptor);
    }

    [Fact]
    public void SyncVoidOnlyInterceptor_IsInvokedForSyncVoidMethods()
    {
        // Arrange
        var interceptor = new SyncVoidOnlyInterceptor();
        var interceptors = new object[] { interceptor };
        var targetCalled = false;

        // Act - simulate the generated pipeline pattern
        ExecuteWithVoidInterceptors(
            interceptors,
            _context,
            _ =>
            {
                targetCalled = true;
            });

        // Assert
        Assert.True(interceptor.WasCalled, "sync void interceptor should be called for sync void methods");
        Assert.True(targetCalled, "target should be called");
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
            _ =>
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
            _ =>
            {
                targetCalled = true;
                return new ValueTask<string>("result");
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
            _ =>
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
            _ => GetStream(1, 2, 3)))
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
        Assert.IsAssignableFrom<ISyncVoidFacadeInterceptor>(interceptor);
        Assert.IsAssignableFrom<ISyncFacadeInterceptor>(interceptor);
        Assert.IsAssignableFrom<IAsyncVoidFacadeInterceptor>(interceptor);
        Assert.IsAssignableFrom<IAsyncFacadeInterceptor>(interceptor);
        Assert.IsAssignableFrom<IStreamFacadeInterceptor>(interceptor);
        Assert.IsAssignableFrom<IFacadeInterceptor>(interceptor);
    }

    [Fact]
    public void FullInterceptor_IsInvokedForSyncResultMethods()
    {
        // Arrange
        var interceptor = new FullInterceptor();
        var interceptors = new object[] { interceptor };
        var targetCalled = false;

        // Act
        ExecuteWithInterceptors<string>(
            interceptors,
            _context,
            _ =>
            {
                targetCalled = true;
                return "result";
            });

        // Assert
        Assert.True(interceptor.SyncResultCalled, "full interceptor should be called for sync result methods");
        Assert.True(targetCalled);
    }

    [Fact]
    public void FullInterceptor_IsInvokedForSyncVoidMethods()
    {
        // Arrange
        var interceptor = new FullInterceptor();
        var interceptors = new object[] { interceptor };
        var targetCalled = false;

        // Act
        ExecuteWithVoidInterceptors(
            interceptors,
            _context,
            _ =>
            {
                targetCalled = true;
            });

        // Assert
        Assert.True(interceptor.SyncVoidCalled, "full interceptor should be called for sync void methods");
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
            _ =>
            {
                targetCalled = true;
                return new ValueTask<string>("result");
            });

        // Assert
        Assert.True(interceptor.AsyncResultCalled, "full interceptor should be called for async methods");
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
            _ => GetStream(1, 2, 3)))
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
        var syncResultInterceptor = new SyncResultOnlyInterceptor();
        var asyncInterceptor = new AsyncOnlyInterceptor();
        var streamInterceptor = new StreamOnlyInterceptor();
        var fullInterceptor = new FullInterceptor();
        var interceptors = new object[] { syncResultInterceptor, asyncInterceptor, streamInterceptor, fullInterceptor };

        // Act
        ExecuteWithInterceptors<string>(
            interceptors,
            _context,
            _ => "result");

        // Assert
        Assert.True(syncResultInterceptor.WasCalled, "sync result interceptor should be called");
        Assert.False(asyncInterceptor.WasCalled, "async-only interceptor should be skipped");
        Assert.False(streamInterceptor.WasCalled, "stream-only interceptor should be skipped");
        Assert.True(fullInterceptor.SyncResultCalled, "full interceptor should be called");
    }

    [Fact]
    public async Task MixedInterceptors_OnlyAsyncInterceptorsInvokedForAsyncMethods()
    {
        // Arrange
        var syncResultInterceptor = new SyncResultOnlyInterceptor();
        var asyncInterceptor = new AsyncOnlyInterceptor();
        var streamInterceptor = new StreamOnlyInterceptor();
        var fullInterceptor = new FullInterceptor();
        var interceptors = new object[] { syncResultInterceptor, asyncInterceptor, streamInterceptor, fullInterceptor };

        // Act
        await ExecuteWithInterceptorsAsync<string>(
            interceptors,
            _context,
            _ => new ValueTask<string>("result"));

        // Assert
        Assert.False(syncResultInterceptor.WasCalled, "sync result interceptor should be skipped");
        Assert.True(asyncInterceptor.WasCalled, "async-only interceptor should be called");
        Assert.False(streamInterceptor.WasCalled, "stream-only interceptor should be skipped");
        Assert.True(fullInterceptor.AsyncResultCalled, "full interceptor should be called");
    }

    [Fact]
    public async Task MixedInterceptors_OnlyStreamInterceptorsInvokedForStreamMethods()
    {
        // Arrange
        var syncResultInterceptor = new SyncResultOnlyInterceptor();
        var asyncInterceptor = new AsyncOnlyInterceptor();
        var streamInterceptor = new StreamOnlyInterceptor();
        var fullInterceptor = new FullInterceptor();
        var interceptors = new object[] { syncResultInterceptor, asyncInterceptor, streamInterceptor, fullInterceptor };
        var items = new List<int>();

        // Act
        await foreach (var item in ExecuteWithInterceptorsStream<int>(
            interceptors,
            _context,
            _ => GetStream(1, 2, 3)))
        {
            items.Add(item);
        }

        // Assert
        Assert.False(syncResultInterceptor.WasCalled, "sync result interceptor should be skipped");
        Assert.False(asyncInterceptor.WasCalled, "async-only interceptor should be skipped");
        Assert.True(streamInterceptor.WasCalled, "stream-only interceptor should be called");
        Assert.True(fullInterceptor.StreamCalled, "full interceptor should be called");
    }

    #endregion

    #region Pipeline Methods (Simulating Generated Code)

    /// <summary>
    /// Simulates the generated ExecuteWithVoidInterceptors method with ISP-compliant interface checks.
    /// </summary>
    private static void ExecuteWithVoidInterceptors(
        object[] interceptors,
        FacadeInvocationContext context,
        FacadeVoidInvocationDelegate target)
    {
        var index = 0;

        FacadeVoidInvocationDelegate BuildPipeline()
        {
            if (index >= interceptors.Length)
                return target;

            var currentIndex = index++;
            var next = BuildPipeline();

            if (interceptors[currentIndex] is ISyncVoidFacadeInterceptor syncVoid)
                return handlers => syncVoid.Intercept(context, nextHandlers => next(nextHandlers ?? handlers));

            return next;
        }

        BuildPipeline()(null);
    }

    /// <summary>
    /// Simulates the generated ExecuteWithInterceptors method with ISP-compliant interface checks.
    /// </summary>
    private static TResult ExecuteWithInterceptors<TResult>(
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
                return handlers => sync.Intercept(context, nextHandlers => next(nextHandlers ?? handlers));

            return next;
        }

        return BuildPipeline()(null);
    }

    /// <summary>
    /// Simulates the generated ExecuteWithInterceptorsAsync method with ISP-compliant interface checks.
    /// </summary>
    private static async ValueTask<TResult> ExecuteWithInterceptorsAsync<TResult>(
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
                return handlers => async.InterceptAsync(context, nextHandlers => next(nextHandlers ?? handlers));

            return next;
        }

        return await BuildPipeline()(null).ConfigureAwait(false);
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
                return handlers => stream.InterceptStream(context, nextHandlers => next(nextHandlers ?? handlers));

            return next;
        }

        return BuildPipeline()(null);
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
    /// Interceptor that only implements ISyncVoidFacadeInterceptor.
    /// </summary>
    private class SyncVoidOnlyInterceptor : ISyncVoidFacadeInterceptor
    {
        public bool WasCalled { get; private set; }

        public void Intercept(FacadeInvocationContext context, FacadeVoidInvocationDelegate next)
        {
            WasCalled = true;
            next();
        }
    }

    /// <summary>
    /// Interceptor that only implements ISyncFacadeInterceptor (for result methods).
    /// </summary>
    private class SyncResultOnlyInterceptor : ISyncFacadeInterceptor
    {
        public bool WasCalled { get; private set; }

        public TResult Intercept<TResult>(FacadeInvocationContext context, FacadeInvocationDelegate<TResult> next)
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

        public ValueTask<TResult> InterceptAsync<TResult>(FacadeInvocationContext context, FacadeAsyncInvocationDelegate<TResult> next)
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
    /// Interceptor that implements the full IFacadeInterceptor interface (all five).
    /// </summary>
    private class FullInterceptor : IFacadeInterceptor
    {
        public bool SyncVoidCalled { get; private set; }
        public bool SyncResultCalled { get; private set; }
        public bool AsyncVoidCalled { get; private set; }
        public bool AsyncResultCalled { get; private set; }
        public bool StreamCalled { get; private set; }

        public void Intercept(FacadeInvocationContext context, FacadeVoidInvocationDelegate next)
        {
            SyncVoidCalled = true;
            next();
        }

        public TResult Intercept<TResult>(FacadeInvocationContext context, FacadeInvocationDelegate<TResult> next)
        {
            SyncResultCalled = true;
            return next();
        }

        public Task InterceptAsync(FacadeInvocationContext context, FacadeAsyncVoidInvocationDelegate next)
        {
            AsyncVoidCalled = true;
            return next();
        }

        public ValueTask<TResult> InterceptAsync<TResult>(FacadeInvocationContext context, FacadeAsyncInvocationDelegate<TResult> next)
        {
            AsyncResultCalled = true;
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
