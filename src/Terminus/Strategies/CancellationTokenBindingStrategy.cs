using System.Threading;

namespace Terminus.Strategies;

public sealed class CancellationTokenBindingStrategy : IParameterBindingStrategy
{
    public bool CanBind(ParameterBindingContext context) 
        => context.ParameterType == typeof(CancellationToken);
    
    public object? Bind(ParameterBindingContext context) 
        => context.CancellationToken;
}