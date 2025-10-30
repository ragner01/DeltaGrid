namespace IOC.Core.Domain.Pipeline;

public sealed class Segment
{
    public string SegmentId { get; }
    public string UpstreamMeterId { get; }
    public string DownstreamMeterId { get; }
    public double ElevationDelta_m { get; } // downstream - upstream
    public double Temperature_C { get; }

    public Segment(string segmentId, string upstreamMeterId, string downstreamMeterId, double elevationDelta_m, double temperature_C)
    {
        SegmentId = segmentId; UpstreamMeterId = upstreamMeterId; DownstreamMeterId = downstreamMeterId; ElevationDelta_m = elevationDelta_m; Temperature_C = temperature_C;
    }
}

public sealed record MeterUncertainty(string MeterId, double Percent);

public sealed record SegmentBaseline(string SegmentId, double MeanBalance_m3_s, double StdBalance_m3_s, DateTimeOffset AsOf);

public sealed class LeakIncident
{
    public Guid Id { get; } = Guid.NewGuid();
    public string SegmentId { get; }
    public DateTimeOffset StartedAt { get; }
    public double Confidence { get; }
    public string EstimatedLocationHint { get; }

    public LeakIncident(string segmentId, DateTimeOffset startedAt, double confidence, string estimatedLocationHint)
    {
        SegmentId = segmentId; StartedAt = startedAt; Confidence = confidence; EstimatedLocationHint = estimatedLocationHint;
    }
}
