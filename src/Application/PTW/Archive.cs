using IOC.Core.Domain.PTW;

namespace IOC.Application.PTW;

public sealed record PermitArchiveRecord(Guid PermitId, string State, string PayloadHash, string PrevHash, string ChainHash, DateTimeOffset At);

public interface IPermitArchive
{
    Task AppendAsync(PermitArchiveRecord record, CancellationToken ct);
}
