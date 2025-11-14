using System.Collections.Generic;
using Terminus;

namespace Demo
{
    [ScopedEntryPointFacade]
    public partial interface IFacade;

    public class TestEntryPoints
    {
        [EntryPoint]
        public async IAsyncEnumerable<int> Stream()
        {
            yield return 1;
        }
    }
}