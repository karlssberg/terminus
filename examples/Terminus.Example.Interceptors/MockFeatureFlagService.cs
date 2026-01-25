using Terminus.Interceptors.Abstractions;

namespace Terminus.Example.Interceptors;

/// <summary>
/// Simple in-memory feature flag service for demonstration purposes.
/// </summary>
public class MockFeatureFlagService : IFeatureFlagService
{
    private readonly HashSet<string> _enabledFeatures = new(StringComparer.OrdinalIgnoreCase);

    public void EnableFeature(string featureName) => _enabledFeatures.Add(featureName);

    public void DisableFeature(string featureName) => _enabledFeatures.Remove(featureName);

    public bool IsEnabled(string featureName) => _enabledFeatures.Contains(featureName);

    public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IsEnabled(featureName));
    }
}
