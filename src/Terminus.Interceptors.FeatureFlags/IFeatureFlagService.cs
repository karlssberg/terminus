namespace Terminus.Interceptors.FeatureFlags;

/// <summary>
/// Provides feature flag evaluation for controlling feature availability at runtime.
/// </summary>
/// <remarks>
/// Implement this interface to integrate with your feature flag system (e.g., LaunchDarkly, Azure App Configuration, custom solution).
/// The service is used by <see cref="FeatureFlagInterceptor"/> to check if a feature is enabled before executing facade methods.
/// </remarks>
public interface IFeatureFlagService
{
    /// <summary>
    /// Determines whether a feature is enabled synchronously.
    /// </summary>
    /// <param name="featureName">The name of the feature to check.</param>
    /// <returns><c>true</c> if the feature is enabled; otherwise, <c>false</c>.</returns>
    bool IsEnabled(string featureName);

    /// <summary>
    /// Determines whether a feature is enabled asynchronously.
    /// </summary>
    /// <param name="featureName">The name of the feature to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, with a result of <c>true</c> if the feature is enabled; otherwise, <c>false</c>.</returns>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);
}
