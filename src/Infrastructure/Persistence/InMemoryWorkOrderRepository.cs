using IOC.Application.Work.CreateWorkOrder;
using IOC.Core.Domain.Work;

namespace IOC.Infrastructure.Persistence;

public sealed class InMemoryWorkOrderRepository : IWorkOrderRepository
{
    private static readonly List<WorkOrder> Store = new();

    public Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        Store.Add(workOrder);
        return Task.CompletedTask;
    }
}
