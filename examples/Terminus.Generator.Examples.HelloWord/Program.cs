using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terminus.Attributes;
using Terminus.Generator.Examples.HelloWorld;
// using Terminus.Generated;

var builder = Host.CreateApplicationBuilder(args);

// builder.Services.AddEntryPointsIMediator();

builder.Services.AddHostedService<Service>();
builder.Services.AddSingleton<Listener>();

var host = builder.Build();

// await host.RunAsync();

namespace Terminus.Generator.Examples.HelloWorld
{
    [Terminus.Attributes.EntryPointMediator]
    public partial interface IMediator;

    public class Listener
    {
        [EntryPoint]
        public void Handle(string message) => Console.WriteLine(message);
    }

    public class Service(IServiceProvider provider) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // provider.GetRequiredService<IMediator>().Handle("hello world");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}


