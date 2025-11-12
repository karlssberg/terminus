#nullable enable
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Terminus
{
    public static partial class ServiceCollectionExtensions__Generated
    {
        public static IServiceCollection AddEntryPointFacade<T>(this IServiceCollection services, Action<ParameterBindingStrategyResolver>? configure = null)
            where T : EntryPointAttribute
        {
            switch (typeof(T).FullName)
            {
                case "Terminus.Generator.Examples.Web.IDispatcher":
                    return services.AddEntryPointFacadeFor_Terminus_Generator_Examples_Web_IDispatcher(configure);
            };
            throw new InvalidOperationException($"No entry point discovery strategy found for type '{typeof(T).FullName}'");
        }

        public static IServiceCollection AddEntryPointFacades(this IServiceCollection services, Action<ParameterBindingStrategyResolver>? configure = null)
        {
            services.AddEntryPointFacadeFor_Terminus_Generator_Examples_Web_IDispatcher();
            return services;
        }
    }
}