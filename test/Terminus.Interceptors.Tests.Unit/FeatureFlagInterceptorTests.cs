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

    #region FilterHandlers Tests - Not Aggregated (Single Handler)

    [Fact]
    public void FilterHandlers_WhenNotAggregated_AndFeatureEnabled_ReturnsHandler()
    {
        // Arrange
        var handler = CreateHandler("MyFeature");
        var context = CreateContext(isAggregated: false, handlers: [handler]);

        _featureFlagService.IsEnabled("MyFeature").Returns(true);

        // Act
        var result = _interceptor.FilterHandlers(context, context.Handlers).ToList();

        // Assert
        Assert.Single(result);
        Assert.Same(handler, result[0]);
    }

    [Fact]
    public void FilterHandlers_WhenNotAggregated_AndFeatureDisabled_ThrowsFeatureDisabledException()
    {
        // Arrange
        var handler = CreateHandler("MyFeature");
        var context = CreateContext(isAggregated: false, handlers: [handler]);

        _featureFlagService.IsEnabled("MyFeature").Returns(false);

        // Act & Assert
        var exception = Assert.Throws<FeatureDisabledException>(() =>
            _interceptor.FilterHandlers(context, context.Handlers).ToList());
        Assert.Equal("MyFeature", exception.FeatureName);
    }

    [Fact]
    public void FilterHandlers_WhenNotAggregated_AndNoFeatureName_ReturnsHandler()
    {
        // Arrange - handler has no feature name
        var handler = CreateHandler(featureName: null);
        var context = CreateContext(isAggregated: false, handlers: [handler]);

        // Act
        var result = _interceptor.FilterHandlers(context, context.Handlers).ToList();

        // Assert
        Assert.Single(result);
        Assert.Same(handler, result[0]);
    }

    #endregion

    #region FilterHandlers Tests - Aggregated (Multiple Handlers)

    [Fact]
    public void FilterHandlers_WhenAggregated_FiltersDisabledHandlers()
    {
        // Arrange
        var handler1 = CreateHandler("Feature1");
        var handler2 = CreateHandler("Feature2");
        var handler3 = CreateHandler("Feature3");
        var context = CreateContext(isAggregated: true, handlers: [handler1, handler2, handler3]);

        _featureFlagService.IsEnabled("Feature1").Returns(true);
        _featureFlagService.IsEnabled("Feature2").Returns(false);
        _featureFlagService.IsEnabled("Feature3").Returns(true);

        // Act
        var result = _interceptor.FilterHandlers(context, context.Handlers).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Same(handler1, result[0]);
        Assert.Same(handler3, result[1]);
    }

    [Fact]
    public void FilterHandlers_WhenAggregated_AndAllDisabled_ReturnsEmpty()
    {
        // Arrange
        var handler1 = CreateHandler("Feature1");
        var handler2 = CreateHandler("Feature2");
        var context = CreateContext(isAggregated: true, handlers: [handler1, handler2]);

        _featureFlagService.IsEnabled("Feature1").Returns(false);
        _featureFlagService.IsEnabled("Feature2").Returns(false);

        // Act
        var result = _interceptor.FilterHandlers(context, context.Handlers).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterHandlers_WhenAggregated_AndAllEnabled_ReturnsAll()
    {
        // Arrange
        var handler1 = CreateHandler("Feature1");
        var handler2 = CreateHandler("Feature2");
        var context = CreateContext(isAggregated: true, handlers: [handler1, handler2]);

        _featureFlagService.IsEnabled("Feature1").Returns(true);
        _featureFlagService.IsEnabled("Feature2").Returns(true);

        // Act
        var result = _interceptor.FilterHandlers(context, context.Handlers).ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void FilterHandlers_WhenAggregated_AndNoFeatureNames_ReturnsAll()
    {
        // Arrange - handlers have no feature names
        var handler1 = CreateHandler(featureName: null);
        var handler2 = CreateHandler(featureName: null);
        var context = CreateContext(isAggregated: true, handlers: [handler1, handler2]);

        // Act
        var result = _interceptor.FilterHandlers(context, context.Handlers).ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void FilterHandlers_WhenAggregated_MixedFeatureNamesAndNoNames_FiltersCorrectly()
    {
        // Arrange
        var handler1 = CreateHandler("Feature1");  // Will be disabled
        var handler2 = CreateHandler(null);        // No feature name - always included
        var handler3 = CreateHandler("Feature3");  // Will be enabled
        var context = CreateContext(isAggregated: true, handlers: [handler1, handler2, handler3]);

        _featureFlagService.IsEnabled("Feature1").Returns(false);
        _featureFlagService.IsEnabled("Feature3").Returns(true);

        // Act
        var result = _interceptor.FilterHandlers(context, context.Handlers).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Same(handler2, result[0]); // No feature name
        Assert.Same(handler3, result[1]); // Feature enabled
    }

    #endregion

    #region Helper Methods

    private static FacadeHandlerDescriptor CreateHandler(string? featureName)
    {
        var attribute = featureName != null
            ? new TestFeatureAttribute(featureName)
            : (Attribute)new TestNoFeatureAttribute();

        return new FacadeHandlerDescriptor(
            targetType: typeof(TestHandler),
            methodAttribute: attribute,
            isStatic: false);
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
