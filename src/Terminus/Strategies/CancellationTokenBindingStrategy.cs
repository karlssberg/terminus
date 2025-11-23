using System.Linq;
using System.Threading;

namespace Terminus.Strategies;

public sealed class CancellationTokenBindingStrategy : IParameterBindingStrategy
{
    public bool CanBind(ParameterBindingContext context) 
        => context.ParameterType == typeof(CancellationToken);
    
    public object? BindParameter(ParameterBindingContext context) 
        => context.Arguments.Values.FirstOrDefault(value => value is CancellationToken);
}