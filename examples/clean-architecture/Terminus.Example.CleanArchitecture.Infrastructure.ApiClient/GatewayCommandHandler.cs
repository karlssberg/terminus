using Terminus.Example.CleanArchitecture.Domain;
using Terminus.Example.CleanArchitecture.Domain.Mediation.Gateways;

namespace Terminus.Example.CleanArchitecture.Infrastructure.ApiClient;

public class GatewayCommandHandler : IGateway<Payment.Request>
{
    public Task Execute(Payment.Request request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Executing payment gateway command");
        return Task.CompletedTask;
    }
}