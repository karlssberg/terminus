using Microsoft.Extensions.DependencyInjection;
using Terminus.Attributes;
using Terminus.Generated;
using Terminus.Generator.Examples.HelloWorld;

var services = new ServiceCollection();

services.AddEntryPoints<EntryPointAttribute>();

var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();
    
mediator.Handle("hello world");

namespace Terminus.Generator.Examples.HelloWorld
{
    [AutoGenerate]
    public partial interface IMediator;

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