using Microsoft.Extensions.DependencyInjection;
using Terminus;
using Terminus.Generator.Examples.HelloWorld;

var services = new ServiceCollection();

services.AddEntryPoints();

var serviceProvider = services.BuildServiceProvider();
var facade = serviceProvider.GetRequiredService<IFacade>();
    
facade.Handle("hello world");

namespace Terminus.Generator.Examples.HelloWorld
{
    [Facade]
    public partial interface IFacade;

    public class MyService
    {
        [EntryPoint]
        public void Handle(string message)
        {
            Console.WriteLine($"Handled message: '{message}'");
        }
    }
    
    public class MyOtherService
    {
        [EntryPoint]
        public static Task<string> Query(string message1, string message2,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Queried messages: '{message1}' and '{message2}'");

            return Task.FromResult(message2);
        }
    } 
}