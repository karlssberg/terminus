namespace Terminus.Example.CleanArchitecture.Domain.Mediation.Gateways;

[FacadeOf<GatewayAttribute>]
public partial interface IGateway;

[Gateway]
public interface IGateway<in TRequest>
{
    public Task Execute(TRequest request, CancellationToken cancellationToken = default);
}

[Gateway]
public interface IGateway<in TRequest, TResponse>
{
    public Task<TResponse> Execute(TRequest request, CancellationToken cancellationToken = default);
}