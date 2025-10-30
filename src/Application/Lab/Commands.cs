using MediatR;
using IOC.BuildingBlocks;
using IOC.Core.Domain.Lab;

namespace IOC.Application.Lab;

public interface ILabRepository
{
    Task SaveSampleAsync(Sample sample, CancellationToken ct);
    Task<Sample?> GetSampleAsync(string sampleId, CancellationToken ct);
    Task SaveResultAsync(LabResult result, CancellationToken ct);
    Task<LabResult?> GetActiveResultAsync(string sampleId, CancellationToken ct);
}

public interface ILabPropertySink
{
    Task PushToAllocationAsync(string sourceId, double? api, double? gor, double? wc, CancellationToken ct);
    Task PushToOptimizationAsync(string sourceId, double? api, double? gor, double? viscosity, CancellationToken ct);
}

public interface IPdfSigner
{
    (string Algo, string Signature) Sign(string certificateUrl);
}

public static class Commands
{
    public sealed record PlanSampleCommand(string SampleId, string SourceId, DateTimeOffset PlannedAt, string Barcode) : IRequest<Result>;
    public sealed record MarkCollectedCommand(string SampleId, DateTimeOffset CollectedAt, string Actor) : IRequest<Result>;
    public sealed record ReceiveSampleCommand(string SampleId, string Actor) : IRequest<Result>;

    public sealed record RecordLabResultCommand(
        string SampleId,
        string MethodVersion,
        double? APIGravity_60F,
        double? GasOilRatio_scf_bbl,
        double? WaterCut_frac,
        double? Salinity_ppm,
        double? Viscosity_cP,
        string CertificateUrl,
        string Actor
    ) : IRequest<Result>;

    public sealed record SetQualityFlagCommand(string SampleId, string Flag, string Actor) : IRequest<Result>;
    public sealed record RequestRetestCommand(string SampleId, string Reason, string Actor) : IRequest<Result>;

    public sealed record PushPropertiesCommand(string SampleId) : IRequest<Result>;

    public sealed class Handlers :
        IRequestHandler<PlanSampleCommand, Result>,
        IRequestHandler<MarkCollectedCommand, Result>,
        IRequestHandler<ReceiveSampleCommand, Result>,
        IRequestHandler<RecordLabResultCommand, Result>,
        IRequestHandler<SetQualityFlagCommand, Result>,
        IRequestHandler<RequestRetestCommand, Result>,
        IRequestHandler<PushPropertiesCommand, Result>
    {
        private readonly ILabRepository _repo;
        private readonly ILabPropertySink _sink;
        private readonly IPdfSigner _signer;
        public Handlers(ILabRepository repo, ILabPropertySink sink, IPdfSigner signer) { _repo = repo; _sink = sink; _signer = signer; }

        public async Task<Result> Handle(PlanSampleCommand request, CancellationToken cancellationToken)
        {
            var s = new Sample(Guid.NewGuid(), request.SampleId, request.SourceId, request.PlannedAt, request.Barcode);
            s.AddChainEvent(request.SourceId, "Planned", request.PlannedAt);
            await _repo.SaveSampleAsync(s, cancellationToken);
            return Result.Success();
        }

        public async Task<Result> Handle(MarkCollectedCommand request, CancellationToken cancellationToken)
        {
            var s = await _repo.GetSampleAsync(request.SampleId, cancellationToken);
            if (s is null) return Result.Failure("Sample not found");
            s.MarkCollected(request.CollectedAt);
            s.AddChainEvent(request.Actor, "Collected", request.CollectedAt);
            await _repo.SaveSampleAsync(s, cancellationToken);
            return Result.Success();
        }

        public async Task<Result> Handle(ReceiveSampleCommand request, CancellationToken cancellationToken)
        {
            var s = await _repo.GetSampleAsync(request.SampleId, cancellationToken);
            if (s is null) return Result.Failure("Sample not found");
            s.MarkReceived();
            s.AddChainEvent(request.Actor, "Received", DateTimeOffset.UtcNow);
            await _repo.SaveSampleAsync(s, cancellationToken);
            return Result.Success();
        }

        public async Task<Result> Handle(RecordLabResultCommand request, CancellationToken cancellationToken)
        {
            var s = await _repo.GetSampleAsync(request.SampleId, cancellationToken);
            if (s is null) return Result.Failure("Sample not found");
            var existing = await _repo.GetActiveResultAsync(request.SampleId, cancellationToken);
            existing?.CloseValidity(DateTimeOffset.UtcNow);

            var r = new LabResult(Guid.NewGuid(), request.SampleId, request.MethodVersion, DateTimeOffset.UtcNow, request.APIGravity_60F, request.GasOilRatio_scf_bbl, request.WaterCut_frac, request.Salinity_ppm, request.Viscosity_cP);
            r.AttachCertificate(request.CertificateUrl);
            var sig = _signer.Sign(request.CertificateUrl);
            r.AttachCertificateSignature(sig.Algo, sig.Signature);
            await _repo.SaveResultAsync(r, cancellationToken);
            return Result.Success();
        }

        public async Task<Result> Handle(SetQualityFlagCommand request, CancellationToken cancellationToken)
        {
            var r = await _repo.GetActiveResultAsync(request.SampleId, cancellationToken);
            if (r is null) return Result.Failure("No active results");
            r.SetQuality(request.Flag);
            await _repo.SaveResultAsync(r, cancellationToken);
            return Result.Success();
        }

        public async Task<Result> Handle(RequestRetestCommand request, CancellationToken cancellationToken)
        {
            var s = await _repo.GetSampleAsync(request.SampleId, cancellationToken);
            if (s is null) return Result.Failure("Sample not found");
            s.AddChainEvent(request.Actor, $"RetestRequested:{request.Reason}", DateTimeOffset.UtcNow);
            await _repo.SaveSampleAsync(s, cancellationToken);
            return Result.Success();
        }

        public async Task<Result> Handle(PushPropertiesCommand request, CancellationToken cancellationToken)
        {
            var s = await _repo.GetSampleAsync(request.SampleId, cancellationToken);
            if (s is null) return Result.Failure("Sample not found");
            var r = await _repo.GetActiveResultAsync(request.SampleId, cancellationToken);
            if (r is null) return Result.Failure("No active results");

            var api = r.APIGravity_60F;
            var gor = r.GasOilRatio_scf_bbl is double g && r.WaterCut_frac is double w ? PropertyCalculators.AdjustedGOR(g, w) : r.GasOilRatio_scf_bbl;
            var wc = r.WaterCut_frac is double w2 && r.Salinity_ppm is double sal ? PropertyCalculators.AdjustedWaterCut(w2, sal) : r.WaterCut_frac;

            await _sink.PushToAllocationAsync(s.SourceId, api, gor, wc, cancellationToken);
            await _sink.PushToOptimizationAsync(s.SourceId, api, gor, r.Viscosity_cP, cancellationToken);
            return Result.Success();
        }
    }
}
