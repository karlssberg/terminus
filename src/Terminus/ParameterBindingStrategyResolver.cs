using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Terminus.Exceptions;
using Terminus.Strategies;

namespace Terminus;

public sealed class ParameterBindingStrategyResolver(IServiceProvider serviceProvider, ParameterBindingStrategyCollection collection)
{
    public TParameter ResolveParameter<TParameter>(string parameterName, IBindingContext context)
    {
        var parameterBindingContext = context.ForParameter(parameterName, typeof(TParameter));
        if (parameterBindingContext.GetCustomBinderOrDefault() is {} customBinder)
        {
            return customBinder.BindParameter<TParameter>(parameterBindingContext);
        }

        var parameterBindingStrategy = 
            collection.Strategies
               .Select(serviceProvider.GetRequiredService)
               .OfType<IParameterBindingStrategy>()
               .FirstOrDefault(strategy => strategy.CanBind(parameterBindingContext)) 
           ?? throw new TerminusParameterBindingException("Unable to find a suitable parameter binding strategy");
        
        return (TParameter)parameterBindingStrategy.BindParameter(parameterBindingContext)!;
    }
}