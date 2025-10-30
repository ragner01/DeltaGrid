using MediatR;
using IOC.BuildingBlocks;
using IOC.Core.Domain.Pipeline;

namespace IOC.Application.Pipeline;

public interface ILeakEventPublisher
{
    Task PublishLeakAsync(LeakIncident incident, CancellationToken ct);
}

public sealed record CalibrateSegmentCommand(string SegmentId, IReadOnlyList<double> BalanceSeries_m3_s) : IRequest<Result>;
public sealed record DetectLeakCommand(string SegmentId, double Upstream_m3_s, double Downstream_m3_s, MeterUncertainty UpUnc, MeterUncertainty DnUnc, double ElevationDelta_m, double Temperature_C, DateTimeOffset Ts) : IRequest<Result<LeakIncident>>;

public interface IPipelineRepository
{
    Task SaveBaselineAsync(SegmentBaseline baseline, CancellationToken ct);
    Task<SegmentBaseline?> GetBaselineAsync(string segmentId, CancellationToken ct);
    Task SaveIncidentAsync(LeakIncident incident, CancellationToken ct);
}

public static class BalanceModel
{
    public static double Compensate(double flow_m3_s, double elevationDelta_m, double temperature_C)
    {
        // Simplified compensation placeholder: scale by temperature factor and elevation-induced density change
        var tempFactor = 1.0 + ((temperature_C - 15.0) * 0.0003);
        var elevationFactor = 1.0 + (elevationDelta_m * 0.00001);
        return flow_m3_s * tempFactor * elevationFactor;
    }

    public static double NetBalance(double up_m3_s, double dn_m3_s, MeterUncertainty upUnc, MeterUncertainty dnUnc)
    {
        var net = up_m3_s - dn_m3_s;
        var tol = Math.Abs(up_m3_s) * upUnc.Percent / 100.0 + Math.Abs(dn_m3_s) * dnUnc.Percent / 100.0;
        return Math.Abs(net) <= tol ? 0.0 : net;
    }
}

public static class ChangePoint
{
    public static bool IsChange(double value, double mean, double std, double k = 3.0)
    {
        if (std <= 0) return Math.Abs(value - mean) > 0.001;
        return Math.Abs(value - mean) > k * std;
    }
}

public sealed class LeakHandlers :
    IRequestHandler<CalibrateSegmentCommand, Result>,
    IRequestHandler<DetectLeakCommand, Result<LeakIncident>>
{
    private readonly IPipelineRepository _repo;
    private readonly ILeakEventPublisher _publisher;

    public LeakHandlers(IPipelineRepository repo, ILeakEventPublisher publisher) { _repo = repo; _publisher = publisher; }

    public async Task<Result> Handle(CalibrateSegmentCommand request, CancellationToken cancellationToken)
    {
        if (request.BalanceSeries_m3_s.Count < 20) return Result.Failure("Not enough calibration data");
        var mean = request.BalanceSeries_m3_s.Average();
        var std = Math.Sqrt(request.BalanceSeries_m3_s.Average(v => Math.Pow(v - mean, 2)));
        await _repo.SaveBaselineAsync(new SegmentBaseline(request.SegmentId, mean, std, DateTimeOffset.UtcNow), cancellationToken);
        return Result.Success();
    }

    public async Task<Result<LeakIncident>> Handle(DetectLeakCommand request, CancellationToken cancellationToken)
    {
        var baseline = await _repo.GetBaselineAsync(request.SegmentId, cancellationToken);
        if (baseline is null) return Result<LeakIncident>.Failure("Segment not calibrated");

        var up = BalanceModel.Compensate(request.Upstream_m3_s, 0, request.Temperature_C);
        var dn = BalanceModel.Compensate(request.Downstream_m3_s, request.ElevationDelta_m, request.Temperature_C);
        var net = BalanceModel.NetBalance(up, dn, request.UpUnc, request.DnUnc);

        if (!ChangePoint.IsChange(net, baseline.MeanBalance_m3_s, baseline.StdBalance_m3_s))
        {
            return Result<LeakIncident>.Failure("No leak detected");
        }

        // Confidence: distance from mean in std units, capped
        var z = Math.Abs(net - baseline.MeanBalance_m3_s) / Math.Max(baseline.StdBalance_m3_s, 0.001);
        var confidence = Math.Min(0.99, z / 6.0);
        var hint = net > 0 ? "Between upstream meter and mid-segment" : "Between mid-segment and downstream meter";
        var incident = new LeakIncident(request.SegmentId, request.Ts, Math.Round(confidence, 3), hint);
        await _repo.SaveIncidentAsync(incident, cancellationToken);
        await _publisher.PublishLeakAsync(incident, cancellationToken);
        return Result<LeakIncident>.Success(incident);
    }
}
