namespace Lingual.SharedKernel.Events;

public interface IDomainEventContext
{
    IEnumerable<DomainEventBase> GetDomainEvents();
}