using Microsoft.Extensions.DependencyInjection;
using Terminus;
using Terminus.Generator.Examples.HelloWorld;

var services = new ServiceCollection();

services.AddEntryPoints<EntryPointAttribute>();

var serviceProvider = services.BuildServiceProvider();
var myListener = serviceProvider.GetRequiredService<IMyListener>();
    
myListener.Handle("hello world");

namespace Terminus.Generator.Examples.HelloWorld
{
    [AutoGenerate(typeof(MyListener))]
    public partial interface IMyListener;

    public class MyListener
    {
        [EntryPoint]
        public void Handle(string message)
        {
            Console.WriteLine($"Handled message: '{message}'");
        }

        [EntryPoint]
        public static Task<string> Query(string message1, string message2,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Queried messages: '{message1}' and '{message2}'");

            return Task.FromResult(message2);
        }
    }
}


