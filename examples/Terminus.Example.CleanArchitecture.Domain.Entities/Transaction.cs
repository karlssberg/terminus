using Terminus.Example.CleanArchitecture.Domain.Entities.ValueObjects;

namespace Terminus.Example.CleanArchitecture.Domain.Entities;

public class Transaction : IEntity
{
    public EntityId Id { get; }
}