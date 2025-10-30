using System.Collections.Concurrent;
using IOC.Application.Custody;
using IOC.Core.Domain.Custody;

namespace IOC.Infrastructure.Persistence;

public sealed class InMemoryCustodyRepository : ICustodyRepository
{
    private static readonly ConcurrentDictionary<string, FiscalMeter> Meters = new();
    private static readonly ConcurrentDictionary<string, Prover> Provers = new();
    private static readonly ConcurrentDictionary<Guid, ProvingRunResult> Proving = new();
    private static readonly ConcurrentDictionary<string, CustodyTicket> Tickets = new();

    public Task<FiscalMeter?> GetMeterAsync(string meterId, CancellationToken ct)
    {
        Meters.TryGetValue(meterId, out var m); return Task.FromResult(m);
    }

    public Task SaveMeterAsync(FiscalMeter meter, CancellationToken ct)
    {
        Meters[meter.MeterId] = meter; return Task.CompletedTask;
    }

    public Task<Prover?> GetProverAsync(string proverId, CancellationToken ct)
    {
        Provers.TryGetValue(proverId, out var p); return Task.FromResult(p);
    }

    public Task SaveProverAsync(Prover prover, CancellationToken ct)
    {
        Provers[prover.ProverId] = prover; return Task.CompletedTask;
    }

    public Task SaveProvingResultAsync(ProvingRunResult result, CancellationToken ct)
    {
        Proving[result.RunId] = result; return Task.CompletedTask;
    }

    public Task<CustodyTicket?> GetTicketAsync(string ticketNumber, CancellationToken ct)
    {
        Tickets.TryGetValue(ticketNumber, out var t); return Task.FromResult(t);
    }

    public Task SaveTicketAsync(CustodyTicket ticket, CancellationToken ct)
    {
        Tickets[ticket.TicketNumber] = ticket; return Task.CompletedTask;
    }
}
