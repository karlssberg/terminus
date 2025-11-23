#nullable enable
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Terminus
{
    public static partial class ServiceCollectionExtensions__Generated
    {
        public static IServiceCollection AddEntryPoints<T>(this IServiceCollection services, Action<ParameterBindingStrategyCollection>? configure = null)
        {
            switch (typeof(T).FullName)
            {
                case "Terminus.Generator.Examples.Web.IDispatcher":
                    return services.AddEntryPointsFor_Terminus_Generator_Examples_Web_IDispatcher(configure);
            };
            throw new InvalidOperationException($"The type '{typeof(T).FullName}' is not an entry point aggregator");
        }

        public static IServiceCollection AddEntryPoints(this IServiceCollection services, Action<ParameterBindingStrategyCollection>? configure = null)
        {
            services.AddEntryPointsFor_Terminus_Generator_Examples_Web_IDispatcher();
            return services;
        }
    }
}