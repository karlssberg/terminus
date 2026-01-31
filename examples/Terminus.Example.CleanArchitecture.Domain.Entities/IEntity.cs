using Terminus.Example.CleanArchitecture.Domain.Entities.ValueObjects;

namespace Terminus.Example.CleanArchitecture.Domain.Entities;

public interface IEntity
{
    public EntityId Id { get; }
}