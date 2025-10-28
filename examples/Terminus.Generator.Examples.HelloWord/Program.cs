using Microsoft.Extensions.Hosting;
using Terminus;

var builder = Host.CreateApplicationBuilder(args);


public class Listener
{
    [EntryPoint]
    public void Handle(string message) => Console.WriteLine(message);
}

