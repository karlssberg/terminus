using System.Diagnostics;

namespace Terminus.Interceptors.Metrics;

/// <summary>
/// Intercepts facade method invocations to record execution metrics including timing and success/failure counts.
/// </summary>
/// <remarks>
/// This interceptor uses <see cref="IMetricsRecorder"/> to record method execution time and outcome.
/// It works with all method types: synchronous, asynchronous, and streaming.
/// </remarks>
public class MetricsInterceptor(IMetricsRecorder metricsRecorder) : FacadeInterceptor
{
    private readonly IMetricsRecorder _metricsRecorder = metricsRecorder ?? throw new ArgumentNullException(nameof(metricsRecorder));

    /// <summary>
    /// Intercepts synchronous facade method invocations (void or result methods).
    /// </summary>
    public override TResult Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var result = next();
            success = true;
            return result;
        }
        finally
        {
            stopwatch.Stop();
            _metricsRecorder.RecordInvocation(context.Method.Name, stopwatch.Elapsed, success);
        }
    }

    /// <summary>
    /// Intercepts asynchronous facade method invocations (Task or Task&lt;T&gt; methods).
    /// </summary>
    public override async ValueTask<TResult> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var result = await next();
            success = true;
            return result;
        }
        finally
        {
            stopwatch.Stop();
            _metricsRecorder.RecordInvocation(context.Method.Name, stopwatch.Elapsed, success);
        }
    }

    /// <summary>
    /// Intercepts streaming facade method invocations (IAsyncEnumerable&lt;T&gt; methods).
    /// </summary>
    public override async IAsyncEnumerable<TItem> InterceptStream<TItem>(
        FacadeInvocationContext context,
        FacadeStreamInvocationDelegate<TItem> next)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            await foreach (var item in next())
            {
                yield return item;
            }

            success = true;
        }
        finally
        {
            stopwatch.Stop();
            _metricsRecorder.RecordInvocation(context.Method.Name, stopwatch.Elapsed, success);
        }
    }
}
