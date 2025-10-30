using MediatR;
using IOC.BuildingBlocks;
using IOC.Core.Domain.Custody;

namespace IOC.Application.Custody;

public interface ICustodyRepository
{
    Task<FiscalMeter?> GetMeterAsync(string meterId, CancellationToken ct);
    Task SaveMeterAsync(FiscalMeter meter, CancellationToken ct);
    Task<Prover?> GetProverAsync(string proverId, CancellationToken ct);
    Task SaveProverAsync(Prover prover, CancellationToken ct);
    Task SaveProvingResultAsync(ProvingRunResult result, CancellationToken ct);
    Task<CustodyTicket?> GetTicketAsync(string ticketNumber, CancellationToken ct);
    Task SaveTicketAsync(CustodyTicket ticket, CancellationToken ct);
}

public static class Commands
{
    public sealed record RegisterMeterCommand(string MeterId, string Model, string Fluid, string UnitSystem) : IRequest<Result>;
    public sealed record RegisterProverCommand(string ProverId, double BaseVolume_m3) : IRequest<Result>;

    public sealed record RunProvingCommand(ProvingRunInput Input) : IRequest<Result<ProvingRunResult>>;

    public sealed record GenerateTicketCommand(
        string TicketNumber,
        string MeterId,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        double StandardVolume_m3,
        string CreatedBy
    ) : IRequest<Result<string>>; // returns ticket number

    public sealed record ApproveTicketCommand(string TicketNumber) : IRequest<Result>;

    public sealed class Handlers :
        IRequestHandler<RegisterMeterCommand, Result>,
        IRequestHandler<RegisterProverCommand, Result>,
        IRequestHandler<RunProvingCommand, Result<ProvingRunResult>>,
        IRequestHandler<GenerateTicketCommand, Result<string>>,
        IRequestHandler<ApproveTicketCommand, Result>
    {
        private readonly ICustodyRepository _repo;
        public Handlers(ICustodyRepository repo) { _repo = repo; }

        public async Task<Result> Handle(RegisterMeterCommand request, CancellationToken cancellationToken)
        {
            await _repo.SaveMeterAsync(new FiscalMeter(Guid.NewGuid(), request.MeterId, request.Model, request.Fluid, request.UnitSystem), cancellationToken);
            return Result.Success();
        }

        public async Task<Result> Handle(RegisterProverCommand request, CancellationToken cancellationToken)
        {
            await _repo.SaveProverAsync(new Prover(Guid.NewGuid(), request.ProverId, request.BaseVolume_m3), cancellationToken);
            return Result.Success();
        }

        public async Task<Result<ProvingRunResult>> Handle(RunProvingCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            var meter = await _repo.GetMeterAsync(input.MeterId, cancellationToken);
            var prover = await _repo.GetProverAsync(input.ProverId, cancellationToken);
            if (meter is null) return Result<ProvingRunResult>.Failure("Unknown meter");
            if (prover is null) return Result<ProvingRunResult>.Failure("Unknown prover");

            // Composite CTPL factor (placeholder)
            var ctpl = CTPL.Composite(input.APIGravity_60F, input.ObservedTemperature_C, input.ObservedPressure_kPag);

            // Adjust observed volume to standard, apply meter factor iteration (simplified one-step)
            var standardVol = input.ObservedVolume_m3 * ctpl;
            var mfFinal = input.MeterFactorInitial * (prover.BaseVolume_m3 / Math.Max(standardVol, 1e-6));

            var result = new ProvingRunResult(Guid.NewGuid(), input.MeterId, input.ProverId, mfFinal, ctpl, standardVol, DateTimeOffset.UtcNow);
            await _repo.SaveProvingResultAsync(result, cancellationToken);
            return Result<ProvingRunResult>.Success(result);
        }

        public async Task<Result<string>> Handle(GenerateTicketCommand request, CancellationToken cancellationToken)
        {
            var ticket = new CustodyTicket(Guid.NewGuid(), request.TicketNumber, request.MeterId, request.PeriodStart, request.PeriodEnd, request.StandardVolume_m3, request.CreatedBy, DateTimeOffset.UtcNow);
            // Compute a simple immutable hash for the ticket content
            var payload = $"{ticket.TicketNumber}|{ticket.MeterId}|{ticket.PeriodStart:o}|{ticket.PeriodEnd:o}|{ticket.StandardVolume_m3:F3}|{ticket.CreatedBy}|{ticket.CreatedAt:o}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = BitConverter.ToString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload))).Replace("-", "").ToLowerInvariant();
            ticket.SetArtifacts($"/artifacts/custody/{ticket.TicketNumber}.pdf", hash);
            await _repo.SaveTicketAsync(ticket, cancellationToken);
            return Result<string>.Success(ticket.TicketNumber);
        }

        public async Task<Result> Handle(ApproveTicketCommand request, CancellationToken cancellationToken)
        {
            var t = await _repo.GetTicketAsync(request.TicketNumber, cancellationToken);
            if (t is null) return Result.Failure("Ticket not found");
            t.Approve();
            await _repo.SaveTicketAsync(t, cancellationToken);
            return Result.Success();
        }
    }
}
