namespace IOC.Core.Domain.Lab;

using IOC.BuildingBlocks;

public sealed class Sample : Entity
{
    public string SampleId { get; }
    public string SourceId { get; } // well/battery/line id
    public DateTimeOffset PlannedAt { get; }
    public DateTimeOffset? CollectedAt { get; private set; }
    public string Barcode { get; }
    public string Status { get; private set; } = "Planned"; // Planned, Collected, Received, Closed
    public List<ChainEvent> ChainOfCustody { get; } = new();

    public Sample(Guid id, string sampleId, string sourceId, DateTimeOffset plannedAt, string barcode) : base(id)
    {
        SampleId = Guard.Against.NullOrWhiteSpace(sampleId, nameof(sampleId));
        SourceId = Guard.Against.NullOrWhiteSpace(sourceId, nameof(sourceId));
        PlannedAt = plannedAt;
        Barcode = Guard.Against.NullOrWhiteSpace(barcode, nameof(barcode));
    }

    public void MarkCollected(DateTimeOffset collectedAt)
    {
        CollectedAt = collectedAt; Status = "Collected";
    }

    public void MarkReceived()
    {
        Status = "Received";
    }

    public void AddChainEvent(string actor, string action, DateTimeOffset ts)
    {
        ChainOfCustody.Add(new ChainEvent(actor, action, ts));
    }
}

public sealed record ChainEvent(string Actor, string Action, DateTimeOffset Timestamp);

public sealed class LabResult : Entity
{
    public string SampleId { get; }
    public string MethodVersion { get; } // e.g. ASTM DXXX v2.1
    public DateTimeOffset EffectiveFrom { get; }
    public DateTimeOffset? EffectiveTo { get; private set; }
    public string CertificateUrl { get; private set; } = string.Empty;
    public string CertificateSignature { get; private set; } = string.Empty;
    public string SignatureAlgo { get; private set; } = string.Empty;
    public string QualityFlag { get; private set; } = "Valid"; // Valid, Suspect, Rejected

    // Primary properties
    public double? APIGravity_60F { get; }
    public double? GasOilRatio_scf_bbl { get; }
    public double? WaterCut_frac { get; }
    public double? Salinity_ppm { get; }
    public double? Viscosity_cP { get; }

    public LabResult(Guid id, string sampleId, string methodVersion, DateTimeOffset effectiveFrom, double? api, double? gor, double? wc, double? salinity, double? viscosity) : base(id)
    {
        SampleId = Guard.Against.NullOrWhiteSpace(sampleId, nameof(sampleId));
        MethodVersion = Guard.Against.NullOrWhiteSpace(methodVersion, nameof(methodVersion));
        EffectiveFrom = effectiveFrom;
        APIGravity_60F = api; GasOilRatio_scf_bbl = gor; WaterCut_frac = wc; Salinity_ppm = salinity; Viscosity_cP = viscosity;
    }

    public void CloseValidity(DateTimeOffset to) => EffectiveTo = to;
    public void AttachCertificate(string url) => CertificateUrl = Guard.Against.NullOrWhiteSpace(url, nameof(url));
    public void AttachCertificateSignature(string algo, string signature)
    {
        SignatureAlgo = Guard.Against.NullOrWhiteSpace(algo, nameof(algo));
        CertificateSignature = Guard.Against.NullOrWhiteSpace(signature, nameof(signature));
    }
    public void SetQuality(string flag) => QualityFlag = Guard.Against.NullOrWhiteSpace(flag, nameof(flag));
}

public static class PropertyCalculators
{
    // Simplified derived property adjustments; documentation covers exact constraints/limits
    public static double ShrinkageFactor(double api) => api >= 0 ? (api > 40 ? 0.985 : 0.992) : 0.99;
    public static double AdjustedGOR(double gor_scf_bbl, double wc_frac) => gor_scf_bbl * (1.0 - Math.Clamp(wc_frac, 0, 0.95) * 0.02);
    public static double AdjustedWaterCut(double wc_frac, double salinity_ppm) => wc_frac * (1.0 + Math.Clamp(salinity_ppm / 200000.0, 0, 0.1));
}
