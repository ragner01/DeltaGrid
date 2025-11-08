using IOC.Core.Domain.Well;

namespace IOC.Application.Well;

public interface IWellRepository
{
    Task<Core.Domain.Well.Well?> GetAsync(Guid id, CancellationToken ct);
    Task SaveAsync(Core.Domain.Well.Well well, CancellationToken ct);
}

