namespace IOC.Events;

public enum Severity { Info, Low, Medium, High, Critical }
public enum Consequence { Safety, Environmental, Production, Integrity, Compliance, Other }

public sealed record RawAlarm(string Source, string TagId, string Message, int? Code, DateTimeOffset OccurredAt, string Quality, Dictionary<string,string>? Attributes);

public sealed record CanonicalEvent(
    Guid Id,
    string TenantId,
    string SiteId,
    string AssetId,
    Severity Severity,
    Consequence Consequence,
    string Category,
    string Message,
    string Source,
    string TagId,
    DateTimeOffset OccurredAt,
    DateTimeOffset RoutedAt,
    int Priority,
    string Fingerprint,
    bool Shelved,
    string Status // New, Acknowledged, Escalated, Resolved
);

public sealed record AckRequest(Guid EventId, string UserId, DateTimeOffset When);
