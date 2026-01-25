using Terminus.Interceptors.Abstractions;

namespace Terminus.Example.Interceptors;

/// <summary>
/// Simple console-based metrics recorder for demonstration purposes.
/// </summary>
public class MockMetricsRecorder : IMetricsRecorder
{
    public void RecordInvocation(string methodName, TimeSpan duration, bool success)
    {
        var status = success ? "SUCCESS" : "FAILURE";
        Console.WriteLine($"[METRICS] {methodName}: {duration.TotalMilliseconds:F2}ms - {status}");
    }

    public IDisposable BeginMeasurement(string methodName)
    {
        return new MeasurementScope(this, methodName);
    }

    private class MeasurementScope(MockMetricsRecorder recorder, string methodName) : IDisposable
    {
        private readonly System.Diagnostics.Stopwatch _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        private bool _success = true;

        public void MarkFailed() => _success = false;

        public void Dispose()
        {
            _stopwatch.Stop();
            recorder.RecordInvocation(methodName, _stopwatch.Elapsed, _success);
        }
    }
}
