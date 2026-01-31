using Terminus.Example.CleanArchitecture.Domain;
using Terminus.Example.CleanArchitecture.Domain.Mediation;

namespace Terminus.Example.CleanArchitecture.Application;

public class PaymentUseCase(IDomainOperation operation)
{
    public record struct Request(decimal Amount, string FromAccount, string ToAccount);
    
    public Task SendMoney(Payment.Request request, CancellationToken cancellationToken = default)
        => operation.Pay(request, cancellationToken);
}