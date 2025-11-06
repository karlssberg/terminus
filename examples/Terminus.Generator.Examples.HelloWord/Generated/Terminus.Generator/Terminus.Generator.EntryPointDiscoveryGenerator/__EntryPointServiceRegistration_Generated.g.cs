#nullable enable
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Terminus
{
    public static partial class ServiceCollectionExtensions__Generated
    {
        public static IServiceCollection AddEntryPoints<T>(this IServiceCollection services, Action<ParameterBindingStrategyResolver>? configure = null)
            where T : EntryPointAttribute
        {
            switch (typeof(T).FullName)
            {
                case "Terminus.EntryPointAttribute":
                    return services.AddEntryPointsFor_Terminus_EntryPointAttribute(configure);
            };
            throw new InvalidOperationException($"No entry point discovery strategy found for type '{typeof(T).FullName}'");
        }

        public static IServiceCollection AddEntryPoints(this IServiceCollection services, Action<ParameterBindingStrategyResolver>? configure = null)
        {
            services.AddEntryPointsFor_Terminus_EntryPointAttribute();
            return services;
        }
    }
}