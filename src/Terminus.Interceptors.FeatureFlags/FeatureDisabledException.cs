namespace Terminus.Interceptors.FeatureFlags;

/// <summary>
/// Exception thrown when a facade method is invoked but the associated feature is disabled.
/// </summary>
public class FeatureDisabledException : Exception
{
    /// <summary>
    /// Gets the name of the disabled feature.
    /// </summary>
    public string FeatureName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureDisabledException"/> class.
    /// </summary>
    /// <param name="featureName">The name of the disabled feature.</param>
    public FeatureDisabledException(string featureName)
        : base($"Feature '{featureName}' is disabled and cannot be executed.")
    {
        FeatureName = featureName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureDisabledException"/> class with a custom message.
    /// </summary>
    /// <param name="featureName">The name of the disabled feature.</param>
    /// <param name="message">The custom error message.</param>
    public FeatureDisabledException(string featureName, string message)
        : base(message)
    {
        FeatureName = featureName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureDisabledException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="featureName">The name of the disabled feature.</param>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public FeatureDisabledException(string featureName, string message, Exception innerException)
        : base(message, innerException)
    {
        FeatureName = featureName;
    }
}
