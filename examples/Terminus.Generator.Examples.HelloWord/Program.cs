using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terminus.Attributes;
using Terminus.Generated;
using Terminus.Generator.Examples.HelloWorld;

var services = new ServiceCollection();

services.AddHostedService<Service>();
services.AddEntryPoints<EntryPointAttribute>();

var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMyListener>();
    
mediator.Handle("hello world");

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
        public static Task<string> Query(string message1, string message2, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Queried messages: '{message1}' and '{message2}'");
            
            return Task.FromResult(message2);
        }
    }

    public class Service(IServiceProvider provider) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            provider.GetRequiredService<IMyListener>().Handle("hello world");
            var message = await provider.GetRequiredService<IMyListener>().Query("hello", "world", cancellationToken);
            
            Console.WriteLine($"Return message: '{message}'");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}


