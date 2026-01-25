using NSubstitute;
using Terminus.Interceptors.Abstractions;
using Xunit;

namespace Terminus.Interceptors.Tests.Unit;

/// <summary>
/// Tests for <see cref="FeatureFlagInterceptor"/>.
/// </summary>
public class FeatureFlagInterceptorTests
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly FeatureFlagInterceptor _interceptor;

    public FeatureFlagInterceptorTests()
    {
        _featureFlagService = Substitute.For<IFeatureFlagService>();
        _interceptor = new FeatureFlagInterceptor(_featureFlagService);
    }

    #region Single Handler (Not Aggregated) Tests

    [Fact]
    public void Intercept_WhenNotAggregated_AndFeatureEnabled_CallsNext()
    {
        // Arrange
        var handler = CreateVoidHandler("MyFeature");
        var context = CreateContext(isAggregated: false, handlers: [handler]);
        var nextCalled = false;

        _featureFlagService.IsEnabled("MyFeature").Returns(true);

        // Act
        _interceptor.Intercept(context, handlers =>
        {
            nextCalled = true;
        });

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public void Intercept_WhenNotAggregated_AndFeatureDisabled_ThrowsFeatureDisabledException()
    {
        // Arrange
        var handler = CreateVoidHandler("MyFeature");
        var context = CreateContext(isAggregated: false, handlers: [handler]);

        _featureFlagService.IsEnabled("MyFeature").Returns(false);

        // Act & Assert
        var exception = Assert.Throws<FeatureDisabledException>(() =>
            _interceptor.Intercept(context, _ => { }));
        Assert.Equal("MyFeature", exception.FeatureName);
    }

    [Fact]
    public void Intercept_WhenNotAggregated_AndNoFeatureName_CallsNext()
    {
        // Arrange - handler has no feature name
        var handler = CreateVoidHandlerWithNoFeature();
        var context = CreateContext(isAggregated: false, handlers: [handler]);
        var nextCalled = false;

        // Act
        _interceptor.Intercept(context, handlers =>
        {
            nextCalled = true;
        });

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public void InterceptResult_WhenNotAggregated_AndFeatureEnabled_ReturnsResult()
    {
        // Arrange
        var handler = CreateSyncHandler<string>("MyFeature");
        var context = CreateContext(isAggregated: false, handlers: [handler]);

        _featureFlagService.IsEnabled("MyFeature").Returns(true);

        // Act
        var result = _interceptor.Intercept(context, handlers => "success");

        // Assert
        Assert.Equal("success", result);
    }

    [Fact]
    public void InterceptResult_WhenNotAggregated_AndFeatureDisabled_ThrowsFeatureDisabledException()
    {
        // Arrange
        var handler = CreateSyncHandler<string>("MyFeature");
        var context = CreateContext(isAggregated: false, handlers: [handler]);

        _featureFlagService.IsEnabled("MyFeature").Returns(false);

        // Act & Assert
        var exception = Assert.Throws<FeatureDisabledException>(() =>
            _interceptor.Intercept<string>(context, _ => "success"));
        Assert.Equal("MyFeature", exception.FeatureName);
    }

    #endregion

    #region Aggregated (Multiple Handlers) Tests

    [Fact]
    public void Intercept_WhenAggregated_FiltersDisabledHandlers()
    {
        // Arrange
        var handler1 = CreateVoidHandler("Feature1");
        var handler2 = CreateVoidHandler("Feature2");
        var handler3 = CreateVoidHandler("Feature3");
        var context = CreateContext(isAggregated: true, handlers: [handler1, handler2, handler3]);
        IReadOnlyList<FacadeHandlerDescriptor>? passedHandlers = null;

        _featureFlagService.IsEnabled("Feature1").Returns(true);
        _featureFlagService.IsEnabled("Feature2").Returns(false);
        _featureFlagService.IsEnabled("Feature3").Returns(true);

        // Act
        _interceptor.Intercept(context, handlers =>
        {
            passedHandlers = handlers;
        });

        // Assert
        Assert.NotNull(passedHandlers);
        Assert.Equal(2, passedHandlers.Count);
        Assert.Same(handler1, passedHandlers[0]);
        Assert.Same(handler3, passedHandlers[1]);
    }

    [Fact]
    public void Intercept_WhenAggregated_AndAllDisabled_PassesEmptyList()
    {
        // Arrange
        var handler1 = CreateVoidHandler("Feature1");
        var handler2 = CreateVoidHandler("Feature2");
        var context = CreateContext(isAggregated: true, handlers: [handler1, handler2]);
        IReadOnlyList<FacadeHandlerDescriptor>? passedHandlers = null;

        _featureFlagService.IsEnabled("Feature1").Returns(false);
        _featureFlagService.IsEnabled("Feature2").Returns(false);

        // Act
        _interceptor.Intercept(context, handlers =>
        {
            passedHandlers = handlers;
        });

        // Assert
        Assert.NotNull(passedHandlers);
        Assert.Empty(passedHandlers);
    }

    [Fact]
    public void Intercept_WhenAggregated_AndAllEnabled_PassesAllHandlers()
    {
        // Arrange
        var handler1 = CreateVoidHandler("Feature1");
        var handler2 = CreateVoidHandler("Feature2");
        var context = CreateContext(isAggregated: true, handlers: [handler1, handler2]);
        IReadOnlyList<FacadeHandlerDescriptor>? passedHandlers = null;

        _featureFlagService.IsEnabled("Feature1").Returns(true);
        _featureFlagService.IsEnabled("Feature2").Returns(true);

        // Act
        _interceptor.Intercept(context, handlers =>
        {
            passedHandlers = handlers;
        });

        // Assert
        Assert.NotNull(passedHandlers);
        Assert.Equal(2, passedHandlers.Count);
    }

    [Fact]
    public void Intercept_WhenAggregated_AndNoFeatureNames_PassesAllHandlers()
    {
        // Arrange - handlers have no feature names
        var handler1 = CreateVoidHandlerWithNoFeature();
        var handler2 = CreateVoidHandlerWithNoFeature();
        var context = CreateContext(isAggregated: true, handlers: [handler1, handler2]);
        IReadOnlyList<FacadeHandlerDescriptor>? passedHandlers = null;

        // Act
        _interceptor.Intercept(context, handlers =>
        {
            passedHandlers = handlers;
        });

        // Assert
        Assert.NotNull(passedHandlers);
        Assert.Equal(2, passedHandlers.Count);
    }

    [Fact]
    public void Intercept_WhenAggregated_MixedFeatureNamesAndNoNames_FiltersCorrectly()
    {
        // Arrange
        var handler1 = CreateVoidHandler("Feature1");  // Will be disabled
        var handler2 = CreateVoidHandlerWithNoFeature();  // No feature name - always included
        var handler3 = CreateVoidHandler("Feature3");  // Will be enabled
        var context = CreateContext(isAggregated: true, handlers: [handler1, handler2, handler3]);
        IReadOnlyList<FacadeHandlerDescriptor>? passedHandlers = null;

        _featureFlagService.IsEnabled("Feature1").Returns(false);
        _featureFlagService.IsEnabled("Feature3").Returns(true);

        // Act
        _interceptor.Intercept(context, handlers =>
        {
            passedHandlers = handlers;
        });

        // Assert
        Assert.NotNull(passedHandlers);
        Assert.Equal(2, passedHandlers.Count);
        Assert.Same(handler2, passedHandlers[0]); // No feature name
        Assert.Same(handler3, passedHandlers[1]); // Feature enabled
    }

    #endregion

    #region Helper Methods

    private static FacadeVoidHandlerDescriptor CreateVoidHandler(string featureName)
    {
        var attribute = new TestFeatureAttribute(featureName);
        return new FacadeVoidHandlerDescriptor(
            targetType: typeof(TestHandler),
            methodAttribute: attribute,
            isStatic: false,
            invoke: () => { });
    }

    private static FacadeVoidHandlerDescriptor CreateVoidHandlerWithNoFeature()
    {
        var attribute = new TestNoFeatureAttribute();
        return new FacadeVoidHandlerDescriptor(
            targetType: typeof(TestHandler),
            methodAttribute: attribute,
            isStatic: false,
            invoke: () => { });
    }

    private static FacadeSyncHandlerDescriptor<T> CreateSyncHandler<T>(string featureName)
    {
        var attribute = new TestFeatureAttribute(featureName);
        return new FacadeSyncHandlerDescriptor<T>(
            targetType: typeof(TestHandler),
            methodAttribute: attribute,
            isStatic: false,
            invoke: () => default!);
    }

    private static FacadeInvocationContext CreateContext(
        bool isAggregated,
        FacadeHandlerDescriptor[] handlers)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var method = typeof(ITestFacade).GetMethod(nameof(ITestFacade.DoWork))!;
        var attribute = handlers.Length > 0 ? handlers[0].MethodAttribute : new TestNoFeatureAttribute();
        var properties = new Dictionary<string, object?>();

        return new FacadeInvocationContext(
            serviceProvider,
            method,
            [],
            typeof(TestHandler),
            attribute,
            properties,
            ReturnTypeKind.Void,
            handlers,
            isAggregated: isAggregated);
    }

    #endregion

    #region Test Types

    private class TestFeatureAttribute(string featureName) : Attribute
    {
        public string FeatureName { get; } = featureName;
    }

    private class TestNoFeatureAttribute : Attribute;

    private interface ITestFacade
    {
        void DoWork();
    }

    private class TestHandler;

    #endregion
}
