using MediatR;

namespace IOC.BuildingBlocks;

public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredOn { get; }
}

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
