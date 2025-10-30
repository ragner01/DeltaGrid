using IOC.BuildingBlocks;
using IOC.Core.Domain.Base;

namespace IOC.Core.Domain.Work;

public sealed class WorkOrder : AggregateRoot, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public string Title { get; private set; }
    public string Description { get; private set; }
    public string SiteId { get; private set; }
    public string AssetId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WorkOrder(string title, string description, string siteId, string assetId)
    {
        Title = title;
        Description = description;
        SiteId = siteId;
        AssetId = assetId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static WorkOrder Create(string title, string description, string siteId, string assetId)
    {
        var wo = new WorkOrder(title, description, siteId, assetId);
        wo._domainEvents.Add(new WorkOrderCreated(wo.Id, wo.SiteId, wo.AssetId));
        return wo;
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public sealed record WorkOrderCreated(Guid WorkOrderId, string SiteId, string AssetId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
