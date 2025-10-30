using System.Collections.Generic;

namespace Terminus.Strategies;

public static class DefaultParameterBindingStrategies
{
    public static IEnumerable<IParameterBindingStrategy> Create()
    {
        yield return new ParameterNameBindingStrategy();
        yield return new CancellationTokenBindingStrategy();
    }
}