namespace IOC.Core.Domain.Integrity;

public sealed class Equipment
{
    public string EquipmentId { get; }
    public string SiteId { get; }
    public string AssetId { get; }
    public string Class { get; }

    public Equipment(string equipmentId, string siteId, string assetId, string @class)
    {
        EquipmentId = equipmentId; SiteId = siteId; AssetId = assetId; Class = @class;
    }
}

public sealed class InspectionPlan
{
    public string PlanId { get; }
    public string EquipmentId { get; }
    public DateOnly StartDate { get; }
    public int IntervalMonths { get; }

    public InspectionPlan(string planId, string equipmentId, DateOnly startDate, int intervalMonths)
    {
        PlanId = planId; EquipmentId = equipmentId; StartDate = startDate; IntervalMonths = intervalMonths;
    }

    public DateOnly NextDue(DateOnly last) => last.AddMonths(IntervalMonths);
}

public sealed record ThicknessReading(string EquipmentId, DateOnly Date, string Location, double Thickness_mm);

public sealed record CorrosionRate(string EquipmentId, string Location, double Rate_mm_per_year, DateOnly From, DateOnly To);

public sealed class Anomaly
{
    public Guid Id { get; } = Guid.NewGuid();
    public string EquipmentId { get; }
    public string Location { get; }
    public string Description { get; }
    public string PhotoUri { get; }
    public string Mitigation { get; private set; } = string.Empty;
    public bool Closed { get; private set; }

    public Anomaly(string equipmentId, string location, string description, string photoUri)
    {
        EquipmentId = equipmentId; Location = location; Description = description; PhotoUri = photoUri;
    }

    public void Close(string mitigation) { Mitigation = mitigation; Closed = true; }
}
