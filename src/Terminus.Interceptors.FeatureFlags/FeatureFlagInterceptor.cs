namespace Terminus.Interceptors.FeatureFlags;

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
/// <para>
/// For aggregated methods, disabled handlers are filtered out via the handlers parameter
/// passed to the <c>next</c> delegate.
/// </para>
/// </remarks>
public class FeatureFlagInterceptor(IFeatureFlagService featureFlagService) : FacadeInterceptor
{
    private readonly IFeatureFlagService _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));

    /// <summary>
    /// Intercepts synchronous void facade method invocations.
    /// </summary>
    public override void Intercept(
        FacadeInvocationContext context,
        FacadeVoidInvocationDelegate next)
    {
        if (context.IsAggregated)
        {
            // Filter handlers and pass to next in chain
            var filteredHandlers = FilterHandlers(context).ToList();
            next(filteredHandlers);
            return;
        }

        // Single handler - throw if disabled
        var featureName = ExtractFeatureName(context.MethodAttribute);
        if (featureName != null && !_featureFlagService.IsEnabled(featureName))
        {
            throw new FeatureDisabledException(featureName);
        }
        next();
    }

    /// <summary>
    /// Intercepts synchronous result-returning facade method invocations.
    /// </summary>
    public override TResult Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next)
    {
        if (context.IsAggregated)
        {
            // Filter handlers and pass to next in chain
            var filteredHandlers = FilterHandlers(context).ToList();
            return next(filteredHandlers);
        }

        // Single handler - throw if disabled
        var featureName = ExtractFeatureName(context.MethodAttribute);
        if (featureName != null && !_featureFlagService.IsEnabled(featureName))
        {
            throw new FeatureDisabledException(featureName);
        }
        return next();
    }

    /// <summary>
    /// Intercepts asynchronous void facade method invocations (Task or ValueTask methods).
    /// </summary>
    public override async Task InterceptAsync(
        FacadeInvocationContext context,
        FacadeAsyncVoidInvocationDelegate next)
    {
        if (context.IsAggregated)
        {
            // Filter handlers and pass to next in chain
            var filteredHandlers = await FilterHandlersAsync(context).ConfigureAwait(false);
            await next(filteredHandlers).ConfigureAwait(false);
            
            return;
        }

        // Single handler - throw if disabled
        var featureName = ExtractFeatureName(context.MethodAttribute);
        if (featureName != null && !await _featureFlagService.IsEnabledAsync(featureName).ConfigureAwait(false))
        {
            throw new FeatureDisabledException(featureName);
        }
        
        await next().ConfigureAwait(false);
    }

    /// <summary>
    /// Intercepts asynchronous result-returning facade method invocations (Task&lt;T&gt; or ValueTask&lt;T&gt; methods).
    /// </summary>
    public override async ValueTask<TResult> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next)
    {
        if (context.IsAggregated)
        {
            // Filter handlers and pass to next in chain
            var filteredHandlers = await FilterHandlersAsync(context).ConfigureAwait(false);
            return await next(filteredHandlers).ConfigureAwait(false);
        }

        // Single handler - throw if disabled
        var featureName = ExtractFeatureName(context.MethodAttribute);
        if (featureName != null && !await _featureFlagService.IsEnabledAsync(featureName).ConfigureAwait(false))
        {
            throw new FeatureDisabledException(featureName);
        }
        
        return await next().ConfigureAwait(false);
    }

    /// <summary>
    /// Intercepts streaming facade method invocations (IAsyncEnumerable&lt;T&gt; methods).
    /// </summary>
    public override async IAsyncEnumerable<TItem> InterceptStream<TItem>(
        FacadeInvocationContext context,
        FacadeStreamInvocationDelegate<TItem> next)
    {
        if (context.IsAggregated)
        {
            // Filter handlers and pass to next in chain
            var filteredHandlers = await FilterHandlersAsync(context).ConfigureAwait(false);
            await foreach (var item in next(filteredHandlers))
            {
                yield return item;
            }
            yield break;
        }

        // Single handler - throw if disabled
        var featureName = ExtractFeatureName(context.MethodAttribute);
        if (featureName != null && !await _featureFlagService.IsEnabledAsync(featureName).ConfigureAwait(false))
        {
            throw new FeatureDisabledException(featureName);
        }
        await foreach (var item in next())
        {
            yield return item;
        }
    }

    /// <summary>
    /// Filters handlers based on feature flag status (synchronous version).
    /// </summary>
    private IEnumerable<FacadeHandlerDescriptor> FilterHandlers(FacadeInvocationContext context)
    {
        foreach (var handler in context.Handlers)
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
    /// Filters handlers based on feature flag status (asynchronous version).
    /// </summary>
    private async Task<List<FacadeHandlerDescriptor>> FilterHandlersAsync(FacadeInvocationContext context)
    {
        var result = new List<FacadeHandlerDescriptor>();

        foreach (var handler in context.Handlers)
        {
            var featureName = ExtractFeatureName(handler.MethodAttribute);

            // If no feature name found, include the handler
            if (featureName == null)
            {
                result.Add(handler);
                continue;
            }

            // Include handler only if feature is enabled
            if (await _featureFlagService.IsEnabledAsync(featureName).ConfigureAwait(false))
            {
                result.Add(handler);
            }
        }

        return result;
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
}
