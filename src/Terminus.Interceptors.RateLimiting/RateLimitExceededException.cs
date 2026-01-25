namespace Terminus.Interceptors;

/// <summary>
/// Exception thrown when a facade method invocation exceeds the configured rate limit.
/// </summary>
public class RateLimitExceededException : Exception
{
    /// <summary>
    /// Gets the rate limit key that was exceeded.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class.
    /// </summary>
    /// <param name="key">The rate limit key that was exceeded.</param>
    public RateLimitExceededException(string key)
        : base($"Rate limit exceeded for key '{key}'.")
    {
        Key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class with a custom message.
    /// </summary>
    /// <param name="key">The rate limit key that was exceeded.</param>
    /// <param name="message">The custom error message.</param>
    public RateLimitExceededException(string key, string message)
        : base(message)
    {
        Key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="key">The rate limit key that was exceeded.</param>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public RateLimitExceededException(string key, string message, Exception innerException)
        : base(message, innerException)
    {
        Key = key;
    }
}
