using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terminus.Attributes;
using Terminus.Generated;
using Terminus.Generator.Examples.HelloWorld;

var builder = Host.CreateApplicationBuilder(args);

// builder.Services.AddEntryPointsIMediator();

builder.Services.AddHostedService<Service>();
builder.Services.AddSingleton<Listener>();
builder.Services.AddEntryPointsForIMediator();

var host = builder.Build();

await host.RunAsync();

namespace Terminus.Generator.Examples.HelloWorld
{
    [EntryPointMediator]
    public partial interface IMediator;

    public class Listener
    {
        [EntryPoint]
        public void Handle(string message)
        {
            Console.WriteLine($"Handled message: '{message}'");
        }
        
        [EntryPoint]
        public static Task<string> Query(string message1, string message2, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Queried messages: '{message1}' and '{message2}'");
            return Task.FromResult(message2);
        }
    }

    public class Service(IServiceProvider provider) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            provider.GetRequiredService<IMediator>().Handle("hello world");
            var message = await provider.GetRequiredService<IMediator>().Query("hello", "world", cancellationToken);
            Console.WriteLine($"Return message: '{message}'");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}


