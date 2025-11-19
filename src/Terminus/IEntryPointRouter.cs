using System.Collections.Generic;

namespace Terminus;

public interface IEntryPointRouter<TFacade>
{
    bool IsMatch(IEntryPointDescriptor ep, IReadOnlyDictionary<string, object?> context);
}
