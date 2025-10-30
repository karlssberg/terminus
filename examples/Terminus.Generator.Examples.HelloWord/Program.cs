using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terminus;
using Terminus.Generated;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddEntryPoints();

builder.Services.AddHostedService<Service>();
builder.Services.AddSingleton<Listener>();

var host = builder.Build();

await host.RunAsync();

public class Listener
{
    [EntryPoint]
    public void Handle(string message) => Console.WriteLine(message);
}

public class Service(IServiceProvider provider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

