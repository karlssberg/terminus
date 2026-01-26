using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Terminus.Interceptors.Logging;

/// <summary>
/// Intercepts facade method invocations to log invocation start, completion, and errors.
/// </summary>
/// <remarks>
/// This interceptor uses <see cref="ILogger{TCategoryName}"/> to log method invocations.
/// It logs at Information level for start/completion and Error level for exceptions.
/// It works with all method types: synchronous, asynchronous, and streaming.
/// </remarks>
public class LoggingInterceptor(ILogger<LoggingInterceptor> logger) : FacadeInterceptor
{
    private readonly ILogger<LoggingInterceptor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Intercepts synchronous facade method invocations (void or result methods).
    /// </summary>
    public override TResult? Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next) where TResult : default
    {
        var methodName = context.Method.Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Invoking {MethodName}", methodName);

        try
        {
            var result = next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Completed {MethodName} in {ElapsedMilliseconds}ms",
                methodName,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Failed {MethodName} after {ElapsedMilliseconds}ms: {ErrorMessage}",
                methodName,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Intercepts asynchronous facade method invocations (Task or Task&lt;T&gt; methods).
    /// </summary>
    public override async ValueTask<TResult?> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next) where TResult : default
    {
        var methodName = context.Method.Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Invoking {MethodName} async", methodName);

        try
        {
            var result = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Completed {MethodName} async in {ElapsedMilliseconds}ms",
                methodName,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Failed {MethodName} async after {ElapsedMilliseconds}ms: {ErrorMessage}",
                methodName,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Intercepts streaming facade method invocations (IAsyncEnumerable&lt;T&gt; methods).
    /// </summary>
    public override async IAsyncEnumerable<TItem> InterceptStream<TItem>(
        FacadeInvocationContext context,
        FacadeStreamInvocationDelegate<TItem> next)
    {
        var methodName = context.Method.Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Invoking {MethodName} stream", methodName);

        var itemCount = 0;

        IAsyncEnumerator<TItem>? enumerator = null;
        try
        {
            enumerator = next().GetAsyncEnumerator();

            while (true)
            {
                TItem current;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }
                    current = enumerator.Current;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(
                        ex,
                        "Failed {MethodName} stream after {ElapsedMilliseconds}ms and {ItemCount} items: {ErrorMessage}",
                        methodName,
                        stopwatch.ElapsedMilliseconds,
                        itemCount,
                        ex.Message);
                    throw;
                }

                itemCount++;
                yield return current;
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "Completed {MethodName} stream in {ElapsedMilliseconds}ms with {ItemCount} items",
                methodName,
                stopwatch.ElapsedMilliseconds,
                itemCount);
        }
        finally
        {
            if (enumerator is not null)
            {
                await enumerator.DisposeAsync();
            }
        }
    }
}
