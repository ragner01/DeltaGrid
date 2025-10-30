namespace IOC.Core.Domain.Allocation;

public enum MeterType
{
    Custody,
    Internal
}

public sealed class Meter
{
    public string MeterId { get; }
    public MeterType Type { get; }
    public string SiteId { get; }
    public string AssetId { get; }

    public Meter(string meterId, MeterType type, string siteId, string assetId)
    {
        MeterId = meterId;
        Type = type;
        SiteId = siteId;
        AssetId = assetId;
    }
}

public sealed class Battery
{
    public string BatteryId { get; }
    public string SiteId { get; }
    public string AssetId { get; }
    public IReadOnlyCollection<string> WellIds => _wellIds.AsReadOnly();
    private readonly List<string> _wellIds = new();

    public Battery(string batteryId, string siteId, string assetId, IEnumerable<string> wellIds)
    {
        BatteryId = batteryId;
        SiteId = siteId;
        AssetId = assetId;
        _wellIds.AddRange(wellIds);
    }
}

public sealed record WellTest(string WellId, DateOnly Day, double OilRate_m3_d, double GasRate_m3_d, double WaterRate_m3_d);

public sealed record BatteryMeasurement(string BatteryId, DateOnly Day, double OilMeasured_m3, double GasMeasured_m3, double WaterMeasured_m3);

public sealed record AllocationResult(string WellId, DateOnly Day, double Oil_m3, double Gas_m3, double Water_m3, string Method, int Version);

public sealed class AllocationRun
{
    public Guid RunId { get; } = Guid.NewGuid();
    public DateOnly Day { get; }
    public string BatteryId { get; }
    public string Method { get; }
    public int Version { get; }
    public DateTimeOffset ExecutedAt { get; } = DateTimeOffset.UtcNow;
    public IReadOnlyCollection<AllocationResult> Results => _results.AsReadOnly();
    private readonly List<AllocationResult> _results = new();

    public AllocationRun(DateOnly day, string batteryId, string method, int version, IEnumerable<AllocationResult> results)
    {
        Day = day;
        BatteryId = batteryId;
        Method = method;
        Version = version;
        _results.AddRange(results);
    }
}
