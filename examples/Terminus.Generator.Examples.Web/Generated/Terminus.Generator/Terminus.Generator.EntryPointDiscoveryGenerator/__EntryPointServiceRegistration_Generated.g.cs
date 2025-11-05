#nullable enable
using Microsoft.Extensions.DependencyInjection;
using System;
using Terminus.Attributes;

namespace Terminus.Generated
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEntryPoints<T>(this IServiceCollection services, Action<ParameterBindingStrategyResolver>? configure = null)
            where T : EntryPointAttribute
        {
            switch (typeof(T).FullName)
            {
                case "Terminus.Generator.Examples.Web.MyHttpPostAttribute":
                    return services.AddEntryPointsFor_Terminus_Generator_Examples_Web_MyHttpPostAttribute(configure);
            };
            throw new InvalidOperationException($"No entry point discovery strategy found for type '{typeof(T).FullName}'");
        }
    }
}