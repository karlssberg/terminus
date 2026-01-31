using Terminus.Example.CleanArchitecture.Domain.Mediation;
using Terminus.Example.CleanArchitecture.Domain.Entities.ValueObjects;
using IGateway = Terminus.Example.CleanArchitecture.Domain.Mediation.Gateways.IGateway;

namespace Terminus.Example.CleanArchitecture.Domain;

[DomainOperation]
public class Payment(IGateway gateway)
{
    public record struct Request(AccountId From, AccountId To, Money Amount);
    public record struct GatewayRequest(AccountId From, AccountId To, Money Amount);
    
    public async Task Pay(Request request, CancellationToken cancellationToken = default)
    {
        //await gateway.Execute(new GatewayRequest(request.From, request.To, request.Amount), cancellationToken);
    }
}