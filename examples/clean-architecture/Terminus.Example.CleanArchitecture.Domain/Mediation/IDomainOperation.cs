using Terminus;

namespace Terminus.Example.CleanArchitecture.Domain.Mediation;

[FacadeOf<DomainOperationAttribute>]
public partial interface IDomainOperation;

public interface IDomainOperation<in TRequest>
{
    public Task Execute(TRequest request, CancellationToken cancellationToken = default);
}