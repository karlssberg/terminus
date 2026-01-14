namespace Terminus.Generator.Builders.Strategies;

/// <summary>
/// Factory for selecting the appropriate service resolution strategy.
/// </summary>
internal static class ServiceResolutionStrategyFactory
{
    private static readonly IServiceResolutionStrategy[] Strategies =
    [
        new StaticServiceResolution(),
        new ScopedServiceResolution(),
        new NonScopedServiceResolution()
    ];

    /// <summary>
    /// Gets the appropriate service resolution strategy for the given method.
    /// </summary>
    public static IServiceResolutionStrategy GetStrategy(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo)
    {
        foreach (var strategy in Strategies)
        {
            if (strategy.CanResolve(facadeInfo, methodInfo))
                return strategy;
        }

        // This should never happen given our strategies cover all cases
        throw new InvalidOperationException(
            $"No service resolution strategy found for method {methodInfo.MethodSymbol.Name}");
    }
}
