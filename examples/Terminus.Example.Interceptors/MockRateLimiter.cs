using System.Collections.Concurrent;
using Terminus.Interceptors.Abstractions;

namespace Terminus.Example.Interceptors;

/// <summary>
/// Simple sliding window rate limiter for demonstration purposes.
/// </summary>
public class MockRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _buckets = new();

    public bool TryAcquire(string key, int maxRequests, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var bucket = _buckets.GetOrAdd(key, _ => new Queue<DateTime>());

        lock (bucket)
        {
            // Remove expired timestamps
            while (bucket.Count > 0 && bucket.Peek() < now - window)
            {
                bucket.Dequeue();
            }

            // Check if we can acquire
            if (bucket.Count < maxRequests)
            {
                bucket.Enqueue(now);
                return true;
            }

            return false;
        }
    }

    public Task<bool> TryAcquireAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(TryAcquire(key, maxRequests, window));
    }
}
