using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Terminus;
using Terminus.Example.Interceptors;
using Terminus.Interceptors;
using Terminus.Interceptors.Abstractions;

// Setup DI container
var services = new ServiceCollection();

// Register logging
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

// Register abstractions with mock implementations
services.AddSingleton<IFeatureFlagService, MockFeatureFlagService>();
services.AddSingleton<IRateLimiter, MockRateLimiter>();
services.AddSingleton<IMetricsRecorder, MockMetricsRecorder>();

// Register caching
services.AddMemoryCache();

// Register interceptors
services.AddSingleton<LoggingInterceptor>();
services.AddSingleton<CachingInterceptor>();
services.AddSingleton<MetricsInterceptor>();
services.AddSingleton<FeatureFlagInterceptor>();
services.AddSingleton<RateLimitInterceptor>();
services.AddSingleton<ValidationInterceptor>();

// Register handlers
services.AddScoped<UserHandlers>();

// Register facades
services.AddTerminusFacades();

var serviceProvider = services.BuildServiceProvider();

// Enable some features
var featureFlags = serviceProvider.GetRequiredService<IFeatureFlagService>() as MockFeatureFlagService;
featureFlags?.EnableFeature("get-user");
featureFlags?.EnableFeature("create-user");

Console.WriteLine("=== Terminus Interceptors Example ===\n");

// Example 1: Logging Interceptor
Console.WriteLine("--- Example 1: Logging Interceptor ---");
var loggingFacade = serviceProvider.GetRequiredService<ILoggingExample>();
loggingFacade.GetUser(1);
Console.WriteLine();

// Example 2: Caching Interceptor
Console.WriteLine("--- Example 2: Caching Interceptor ---");
var cachingFacade = serviceProvider.GetRequiredService<ICachingExample>();
Console.WriteLine("First call (cache miss):");
cachingFacade.GetUser(1);
Console.WriteLine("Second call (cache hit):");
cachingFacade.GetUser(1);
Console.WriteLine();

// Example 3: Metrics Interceptor
Console.WriteLine("--- Example 3: Metrics Interceptor ---");
var metricsFacade = serviceProvider.GetRequiredService<IMetricsExample>();
metricsFacade.ProcessData("test");
Console.WriteLine();

// Example 4: Feature Flag Interceptor
Console.WriteLine("--- Example 4: Feature Flag Interceptor ---");
var featureFacade = serviceProvider.GetRequiredService<IFeatureFlagExample>();
try
{
    featureFacade.GetUser(1); // Enabled
    Console.WriteLine("GetUser succeeded (feature enabled)");
}
catch (FeatureDisabledException ex)
{
    Console.WriteLine($"GetUser failed: {ex.Message}");
}

try
{
    featureFacade.DeleteUser(1); // Disabled
}
catch (FeatureDisabledException ex)
{
    Console.WriteLine($"DeleteUser failed (feature disabled): {ex.Message}");
}
Console.WriteLine();

// Example 5: Rate Limiting Interceptor
Console.WriteLine("--- Example 5: Rate Limiting Interceptor ---");
var rateLimitFacade = serviceProvider.GetRequiredService<IRateLimitExample>();
for (int i = 0; i < 5; i++)
{
    try
    {
        rateLimitFacade.DoWork();
        Console.WriteLine($"Request {i + 1} succeeded");
    }
    catch (RateLimitExceededException ex)
    {
        Console.WriteLine($"Request {i + 1} failed: {ex.Message}");
    }
}
Console.WriteLine();

// Example 6: Multiple Interceptors
Console.WriteLine("--- Example 6: Multiple Interceptors (Logging + Metrics) ---");
var combinedFacade = serviceProvider.GetRequiredService<ICombinedExample>();
combinedFacade.ProcessData("test");

Console.WriteLine("\n=== Example Complete ===");

// Custom attribute for logging example
public class LoggingHandlerAttribute : Attribute;

[FacadeOf(typeof(LoggingHandlerAttribute), Interceptors = [typeof(LoggingInterceptor)])]
public partial interface ILoggingExample;

// Custom attribute for caching example
public class CachingHandlerAttribute : Attribute;

[FacadeOf(typeof(CachingHandlerAttribute), Interceptors = [typeof(CachingInterceptor)])]
public partial interface ICachingExample;

// Custom attribute for metrics example
public class MetricsHandlerAttribute : Attribute;

[FacadeOf(typeof(MetricsHandlerAttribute), Interceptors = [typeof(MetricsInterceptor)])]
public partial interface IMetricsExample;

// Custom attribute for feature flag example
public class FeatureHandlerAttribute(string featureName) : Attribute
{
    public string FeatureName { get; } = featureName;
}

[FacadeOf(typeof(FeatureHandlerAttribute), Interceptors = [typeof(FeatureFlagInterceptor)])]
public partial interface IFeatureFlagExample;

// Custom attribute for rate limiting example
public class RateLimitHandlerAttribute(int maxRequests, int windowSeconds) : Attribute
{
    public int MaxRequests { get; } = maxRequests;
    public int WindowSeconds { get; } = windowSeconds;
}

[FacadeOf(typeof(RateLimitHandlerAttribute), Interceptors = [typeof(RateLimitInterceptor)])]
public partial interface IRateLimitExample;

// Custom attribute for combined example
public class CombinedHandlerAttribute : Attribute;

[FacadeOf(typeof(CombinedHandlerAttribute), Interceptors = [typeof(LoggingInterceptor), typeof(MetricsInterceptor)])]
public partial interface ICombinedExample;

// Handler implementations
public class UserHandlers
{
    [LoggingHandler]
    public User GetUser(int id)
    {
        return new User(id, $"User{id}");
    }

    [CachingHandler]
    public User GetCachedUser(int id)
    {
        Console.WriteLine($"  -> Fetching user {id} from database...");
        return new User(id, $"User{id}");
    }

    [MetricsHandler]
    public void ProcessData(string data)
    {
        Console.WriteLine($"  -> Processing: {data}");
    }

    [FeatureHandler("get-user")]
    public User GetUserWithFeature(int id)
    {
        return new User(id, $"User{id}");
    }

    [FeatureHandler("delete-user")]
    public void DeleteUser(int id)
    {
        Console.WriteLine($"  -> Deleting user {id}");
    }

    [RateLimitHandler(maxRequests: 3, windowSeconds: 60)]
    public void DoWork()
    {
        Console.WriteLine("  -> Working...");
    }

    [CombinedHandler]
    public void ProcessWithMultipleInterceptors(string data)
    {
        Console.WriteLine($"  -> Processing with multiple interceptors: {data}");
    }
}

// Extension methods to map facade methods to handler methods
public static class FacadeExtensions
{
    // Logging example
    public static User GetUser(this ILoggingExample facade, int id)
    {
        var handlers = ((IServiceProvider)((dynamic)facade).GetType()
            .GetField("_serviceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(facade)!)
            .GetRequiredService<UserHandlers>();
        return handlers.GetUser(id);
    }

    // Caching example
    public static User GetUser(this ICachingExample facade, int id)
    {
        var handlers = ((IServiceProvider)((dynamic)facade).GetType()
            .GetField("_serviceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(facade)!)
            .GetRequiredService<UserHandlers>();
        return handlers.GetCachedUser(id);
    }

    // Metrics example
    public static void ProcessData(this IMetricsExample facade, string data)
    {
        var handlers = ((IServiceProvider)((dynamic)facade).GetType()
            .GetField("_serviceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(facade)!)
            .GetRequiredService<UserHandlers>();
        handlers.ProcessData(data);
    }

    // Feature flag example
    public static User GetUser(this IFeatureFlagExample facade, int id)
    {
        var handlers = ((IServiceProvider)((dynamic)facade).GetType()
            .GetField("_serviceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(facade)!)
            .GetRequiredService<UserHandlers>();
        return handlers.GetUserWithFeature(id);
    }

    public static void DeleteUser(this IFeatureFlagExample facade, int id)
    {
        var handlers = ((IServiceProvider)((dynamic)facade).GetType()
            .GetField("_serviceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(facade)!)
            .GetRequiredService<UserHandlers>();
        handlers.DeleteUser(id);
    }

    // Rate limit example
    public static void DoWork(this IRateLimitExample facade)
    {
        var handlers = ((IServiceProvider)((dynamic)facade).GetType()
            .GetField("_serviceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(facade)!)
            .GetRequiredService<UserHandlers>();
        handlers.DoWork();
    }

    // Combined example
    public static void ProcessData(this ICombinedExample facade, string data)
    {
        var handlers = ((IServiceProvider)((dynamic)facade).GetType()
            .GetField("_serviceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(facade)!)
            .GetRequiredService<UserHandlers>();
        handlers.ProcessWithMultipleInterceptors(data);
    }
}

public record User(int Id, string Name);
