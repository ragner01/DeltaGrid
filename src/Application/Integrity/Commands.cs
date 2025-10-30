using MediatR;
using IOC.BuildingBlocks;
using IOC.Core.Domain.Integrity;

namespace IOC.Application.Integrity;

public sealed record RecordThicknessCommand(string EquipmentId, DateOnly Date, string Location, double Thickness_mm) : IRequest<Result>;
public sealed record ComputeCorrosionRateQuery(string EquipmentId, string Location) : IRequest<Result<CorrosionRate>>;
public sealed record CreateInspectionPlanCommand(string PlanId, string EquipmentId, DateOnly StartDate, int IntervalMonths) : IRequest<Result>;
public sealed record CreateAnomalyCommand(string EquipmentId, string Location, string Description, string PhotoUri) : IRequest<Result<Guid>>;
public sealed record CloseAnomalyCommand(Guid AnomalyId, string Mitigation) : IRequest<Result>;

public interface IIntegrityRepository
{
    Task SaveReadingAsync(ThicknessReading r, CancellationToken ct);
    Task<IReadOnlyList<ThicknessReading>> GetReadingsAsync(string equipmentId, string location, CancellationToken ct);
    Task SavePlanAsync(InspectionPlan plan, CancellationToken ct);
    Task<InspectionPlan?> GetPlanAsync(string planId, CancellationToken ct);
    Task SaveAnomalyAsync(Anomaly anomaly, CancellationToken ct);
    Task<Anomaly?> GetAnomalyAsync(Guid id, CancellationToken ct);
}

public sealed class IntegrityHandlers :
    IRequestHandler<RecordThicknessCommand, Result>,
    IRequestHandler<ComputeCorrosionRateQuery, Result<CorrosionRate>>,
    IRequestHandler<CreateInspectionPlanCommand, Result>,
    IRequestHandler<CreateAnomalyCommand, Result<Guid>>,
    IRequestHandler<CloseAnomalyCommand, Result>
{
    private readonly IIntegrityRepository _repo;

    public IntegrityHandlers(IIntegrityRepository repo) { _repo = repo; }

    public async Task<Result> Handle(RecordThicknessCommand request, CancellationToken cancellationToken)
    {
        await _repo.SaveReadingAsync(new ThicknessReading(request.EquipmentId, request.Date, request.Location, request.Thickness_mm), cancellationToken);
        return Result.Success();
    }

    public async Task<Result<CorrosionRate>> Handle(ComputeCorrosionRateQuery request, CancellationToken cancellationToken)
    {
        var readings = (await _repo.GetReadingsAsync(request.EquipmentId, request.Location, cancellationToken))
            .OrderBy(r => r.Date).ToList();
        if (readings.Count < 2) return Result<CorrosionRate>.Failure("Not enough readings");
        var first = readings.First();
        var last = readings.Last();
        var years = (last.Date.ToDateTime(TimeOnly.MinValue) - first.Date.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.25;
        if (years <= 0) return Result<CorrosionRate>.Failure("Invalid interval");
        var rate = (first.Thickness_mm - last.Thickness_mm) / years;
        return Result<CorrosionRate>.Success(new CorrosionRate(request.EquipmentId, request.Location, Math.Round(rate, 3), first.Date, last.Date));
    }

    public async Task<Result> Handle(CreateInspectionPlanCommand request, CancellationToken cancellationToken)
    {
        await _repo.SavePlanAsync(new InspectionPlan(request.PlanId, request.EquipmentId, request.StartDate, request.IntervalMonths), cancellationToken);
        return Result.Success();
    }

    public async Task<Result<Guid>> Handle(CreateAnomalyCommand request, CancellationToken cancellationToken)
    {
        var a = new Anomaly(request.EquipmentId, request.Location, request.Description, request.PhotoUri);
        await _repo.SaveAnomalyAsync(a, cancellationToken);
        return Result<Guid>.Success(a.Id);
    }

    public async Task<Result> Handle(CloseAnomalyCommand request, CancellationToken cancellationToken)
    {
        var a = await _repo.GetAnomalyAsync(request.AnomalyId, cancellationToken);
        if (a is null) return Result.Failure("Anomaly not found");
        a.Close(request.Mitigation);
        await _repo.SaveAnomalyAsync(a, cancellationToken);
        return Result.Success();
    }
}
