namespace IOC.Core.Domain.Custody;

using IOC.BuildingBlocks;
using IOC.Core.Domain.Base;

public sealed class FiscalMeter : Entity
{
    public string MeterId { get; }
    public string Model { get; }
    public string Fluid { get; } // Oil/Gas
    public string UnitSystem { get; } // SI/API

    public FiscalMeter(Guid id, string meterId, string model, string fluid, string unitSystem) : base(id)
    {
        MeterId = Guard.Against.NullOrWhiteSpace(meterId, nameof(meterId));
        Model = Guard.Against.NullOrWhiteSpace(model, nameof(model));
        Fluid = Guard.Against.NullOrWhiteSpace(fluid, nameof(fluid));
        UnitSystem = Guard.Against.NullOrWhiteSpace(unitSystem, nameof(unitSystem));
    }
}

public sealed class Prover : Entity
{
    public string ProverId { get; }
    public double BaseVolume_m3 { get; } // calibrated prover volume at base conditions

    public Prover(Guid id, string proverId, double baseVolume_m3) : base(id)
    {
        ProverId = Guard.Against.NullOrWhiteSpace(proverId, nameof(proverId));
        BaseVolume_m3 = Guard.Against.NegativeOrZero(baseVolume_m3, nameof(baseVolume_m3));
    }
}

public sealed record ProvingRunInput(
    string MeterId,
    string ProverId,
    double ObservedVolume_m3,
    double ObservedTemperature_C,
    double ObservedPressure_kPag,
    double APIGravity_60F,
    double MeterFactorInitial
);

public sealed record ProvingRunResult(
    Guid RunId,
    string MeterId,
    string ProverId,
    double MeterFactorFinal,
    double CTPL_Correction,
    double StandardVolume_m3,
    DateTimeOffset Timestamp
);

public static class CTPL
{
    // Simplified CTPL/API correction placeholders (appendix covers exact refs). Values are conservative demo approximations.
    public static double OilShrinkageFactor(double api60) => 1.0 - (api60 > 40 ? 0.001 : 0.0005);
    public static double TemperatureFactor(double tempC) => 1.0 - (tempC - 15.0) * 0.00064; // crude thermal expansion approx
    public static double PressureFactor(double kPag) => 1.0 + (kPag / 1000.0) * 0.0002; // compressibility approx
    public static double Composite(double api60, double tC, double kPag) => OilShrinkageFactor(api60) * TemperatureFactor(tC) * PressureFactor(kPag);
}

public sealed class CustodyTicket : Entity
{
    public string TicketNumber { get; }
    public string MeterId { get; }
    public DateTimeOffset PeriodStart { get; }
    public DateTimeOffset PeriodEnd { get; }
    public double StandardVolume_m3 { get; }
    public string Unit { get; } = "m3";
    public string CreatedBy { get; }
    public DateTimeOffset CreatedAt { get; }
    public string Status { get; private set; } = "Pending"; // Pending, Approved, Rejected
    public string ImmutableHash { get; private set; } = string.Empty;
    public string PdfUrl { get; private set; } = string.Empty;

    public CustodyTicket(Guid id, string ticketNumber, string meterId, DateTimeOffset periodStart, DateTimeOffset periodEnd, double standardVolume_m3, string createdBy, DateTimeOffset createdAt) : base(id)
    {
        TicketNumber = Guard.Against.NullOrWhiteSpace(ticketNumber, nameof(ticketNumber));
        MeterId = Guard.Against.NullOrWhiteSpace(meterId, nameof(meterId));
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        StandardVolume_m3 = standardVolume_m3;
        CreatedBy = Guard.Against.NullOrWhiteSpace(createdBy, nameof(createdBy));
        CreatedAt = createdAt;
    }

    public void SetArtifacts(string pdfUrl, string contentHash)
    {
        PdfUrl = Guard.Against.NullOrWhiteSpace(pdfUrl, nameof(pdfUrl));
        ImmutableHash = Guard.Against.NullOrWhiteSpace(contentHash, nameof(contentHash));
    }

    public void Approve()
    {
        if (Status != "Pending") throw new InvalidOperationException("Only pending tickets can be approved.");
        Status = "Approved";
    }

    public void Reject()
    {
        if (Status != "Pending") throw new InvalidOperationException("Only pending tickets can be rejected.");
        Status = "Rejected";
    }
}
