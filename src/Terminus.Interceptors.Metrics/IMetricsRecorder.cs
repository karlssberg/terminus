namespace Terminus.Interceptors.Metrics;

/// <summary>
/// Provides metrics recording capabilities for tracking facade method invocations.
/// </summary>
/// <remarks>
/// Implement this interface to integrate with your metrics system (e.g., OpenTelemetry, Application Insights, Prometheus, custom solution).
/// The service is used by <see cref="MetricsInterceptor"/> to record execution time, success/failure counts, and other telemetry data.
/// </remarks>
public interface IMetricsRecorder
{
    /// <summary>
    /// Records a completed method invocation with its duration and outcome.
    /// </summary>
    /// <param name="methodName">The name of the method that was invoked.</param>
    /// <param name="duration">The duration of the method execution.</param>
    /// <param name="success"><c>true</c> if the method completed successfully; <c>false</c> if it threw an exception.</param>
    void RecordInvocation(string methodName, TimeSpan duration, bool success);

    /// <summary>
    /// Begins measuring a method invocation and returns a disposable that records the measurement when disposed.
    /// </summary>
    /// <param name="methodName">The name of the method being measured.</param>
    /// <returns>A disposable that records the measurement when disposed.</returns>
    /// <remarks>
    /// This method enables using patterns for automatic measurement:
    /// <code>
    /// using (metricsRecorder.BeginMeasurement("MyMethod"))
    /// {
    ///     // Method execution
    /// }
    /// </code>
    /// </remarks>
    IDisposable BeginMeasurement(string methodName);
}
