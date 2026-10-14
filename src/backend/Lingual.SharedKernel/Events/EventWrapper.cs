using MediatR;

namespace Lingual.SharedKernel.Events;

public sealed class EventWrapper(DomainEventBase @event) : INotification
{
    public DomainEventBase Event { get; } = @event;
}