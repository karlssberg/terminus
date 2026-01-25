using Terminus.Interceptors.Abstractions;

namespace Terminus.Interceptors;

/// <summary>
/// Intercepts facade method invocations to check feature flags before execution.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor uses <see cref="IFeatureFlagService"/> to check if a feature is enabled.
/// The feature name is extracted from a custom attribute on the method (e.g., <c>FeatureAttribute</c>).
/// </para>
/// <para>
/// If the feature is disabled, a <see cref="FeatureDisabledException"/> is thrown.
/// The custom attribute must have a property or field that can be used to identify the feature name.
/// </para>
/// </remarks>
public class FeatureFlagInterceptor(IFeatureFlagService featureFlagService) : FacadeInterceptor, IAggregatableInterceptor
{
    private readonly IFeatureFlagService _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));

    /// <summary>
    /// Intercepts synchronous facade method invocations (void or result methods).
    /// </summary>
    public override TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next) where TResult : default
    {
        var featureName = ExtractFeatureName(context);
        if (featureName != null && !_featureFlagService.IsEnabled(featureName))
        {
            throw new FeatureDisabledException(featureName);
        }

        return next();
    }

    /// <summary>
    /// Intercepts asynchronous facade method invocations (Task or Task&lt;T&gt; methods).
    /// </summary>
    public override async ValueTask<TResult?> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next) where TResult : default
    {
        var featureName = ExtractFeatureName(context);
        if (featureName != null && !await _featureFlagService.IsEnabledAsync(featureName))
        {
            throw new FeatureDisabledException(featureName);
        }

        return await next();
    }

    /// <summary>
    /// Intercepts streaming facade method invocations (IAsyncEnumerable&lt;T&gt; methods).
    /// </summary>
    public override async IAsyncEnumerable<TItem> InterceptStream<TItem>(
        FacadeInvocationContext context,
        FacadeStreamInvocationDelegate<TItem> next)
    {
        var featureName = ExtractFeatureName(context);
        if (featureName != null && !await _featureFlagService.IsEnabledAsync(featureName))
        {
            throw new FeatureDisabledException(featureName);
        }

        await foreach (var item in next())
        {
            yield return item;
        }
    }

    /// <summary>
    /// Filters handlers based on feature flag status.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Behavior depends on whether the method is aggregated:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Not Aggregated (single handler)</b>: Checks the feature flag and throws
    /// <see cref="FeatureDisabledException"/> if the feature is disabled.</description></item>
    /// <item><description><b>Aggregated (multiple handlers)</b>: Filters and returns only handlers
    /// with enabled feature flags (or no feature flag).</description></item>
    /// </list>
    /// </remarks>
    public IEnumerable<FacadeHandlerDescriptor> FilterHandlers(
        FacadeInvocationContext context,
        IReadOnlyList<FacadeHandlerDescriptor> handlers)
    {
        if (!context.IsAggregated)
        {
            // Single handler mode - throw if disabled
            var handler = handlers[0];
            var featureName = ExtractFeatureName(handler.MethodAttribute);

            if (featureName != null && !_featureFlagService.IsEnabled(featureName))
            {
                throw new FeatureDisabledException(featureName);
            }

            yield return handler;
            yield break;
        }

        // Aggregated mode - filter to enabled handlers only
        foreach (var handler in handlers)
        {
            var featureName = ExtractFeatureName(handler.MethodAttribute);

            // If no feature name found, include the handler
            if (featureName == null)
            {
                yield return handler;
                continue;
            }

            // Include handler only if feature is enabled
            if (_featureFlagService.IsEnabled(featureName))
            {
                yield return handler;
            }
        }
    }

    /// <summary>
    /// Extracts the feature name from an attribute.
    /// </summary>
    /// <remarks>
    /// This method looks for common property names on the attribute: FeatureName, Feature, Name, or Key.
    /// If none are found, it returns null and the feature flag check is skipped.
    /// </remarks>
    private static string? ExtractFeatureName(Attribute attribute)
    {
        // Try to extract feature name from common property names
        var type = attribute.GetType();
        var properties = new[] { "FeatureName", "Feature", "Name", "Key" };

        foreach (var propName in properties)
        {
            var property = type.GetProperty(propName);
            if (property?.PropertyType == typeof(string))
            {
                return property.GetValue(attribute) as string;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the feature name from the invocation context's method attribute.
    /// </summary>
    private static string? ExtractFeatureName(FacadeInvocationContext context)
    {
        return ExtractFeatureName(context.MethodAttribute);
    }
}
