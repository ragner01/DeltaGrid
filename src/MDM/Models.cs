namespace IOC.MDM;

public sealed record Authority(string Domain, string System, string Steward);

public sealed record MasterRecord(
    string EntityType,      // Asset, Meter, Well, Unit, Factor, Code
    string Key,             // canonical key
    string Name,
    Dictionary<string, string> Attributes,
    Authority Authority,
    DateTimeOffset Timestamp,
    string Reason
);

public sealed record GoldenRecord(
    string EntityType,
    string Key,
    string Name,
    Dictionary<string, string> Attributes,
    string SourceOfTruthSystem,
    string Steward,
    DateTimeOffset UpdatedAt
);

public sealed record Snapshot(string Id, DateTimeOffset CreatedAt, List<GoldenRecord> Records);

public sealed class AuthorityRegistry
{
    private readonly HashSet<string> _authoritative = new(StringComparer.OrdinalIgnoreCase)
    {
        "Asset:CMMS", "Meter:FlowCal", "Well:ProdDB", "Unit:MDM", "Factor:MDM", "Code:MDM"
    };
    public bool IsAuthoritative(string entityType, string system) => _authoritative.Contains($"{entityType}:{system}");
}

public sealed class ReferenceStore
{
    private readonly List<(string code, string from, string to, double factor)> _units = new()
    {
        ("FLOW_M3S_TO_BBLD", "m3/s", "bbl/d", 543439.65)
    };
    private readonly Dictionary<string, string> _codes = new()
    {
        ["ASSET_CLASS.PUMP"] = "Rotating equipment",
        ["ASSET_CLASS.METER"] = "Instrumentation"
    };
    public IReadOnlyList<object> Units() => _units.Select(u => new { u.code, u.from, u.to, u.factor }).ToList<object>();
    public IReadOnlyDictionary<string, string> Codes() => _codes;
}

public sealed class GoldenRecordStore
{
    private readonly Dictionary<(string entityType, string key), GoldenRecord> _golden = new();
    private readonly AuthorityRegistry _auth;

    public GoldenRecordStore(AuthorityRegistry auth) { _auth = auth; }

    public void Upsert(MasterRecord incoming)
    {
        var k = (incoming.EntityType, incoming.Key);
        if (!_golden.TryGetValue(k, out var existing))
        {
            _golden[k] = new GoldenRecord(incoming.EntityType, incoming.Key, incoming.Name, incoming.Attributes, incoming.Authority.System, incoming.Authority.Steward, incoming.Timestamp);
            return;
        }
        // Survivorship: authoritative system wins; otherwise latest timestamp wins per field
        var isAuthoritative = _auth.IsAuthoritative(incoming.EntityType, incoming.Authority.System);
        var attrs = new Dictionary<string, string>(existing.Attributes);
        foreach (var kv in incoming.Attributes)
        {
            if (isAuthoritative || !attrs.ContainsKey(kv.Key))
            {
                attrs[kv.Key] = kv.Value;
            }
        }
        var name = isAuthoritative ? incoming.Name : existing.Name;
        _golden[k] = new GoldenRecord(existing.EntityType, existing.Key, name, attrs, isAuthoritative ? incoming.Authority.System : existing.SourceOfTruthSystem, isAuthoritative ? incoming.Authority.Steward : existing.Steward, DateTimeOffset.UtcNow);
    }

    public IReadOnlyList<GoldenRecord> All() => _golden.Values.ToList();
}

public sealed class SnapshotStore
{
    private readonly GoldenRecordStore _gr;
    private readonly Dictionary<string, Snapshot> _snaps = new();
    public SnapshotStore(GoldenRecordStore gr) { _gr = gr; }

    public Snapshot CreateSnapshot()
    {
        var id = Guid.NewGuid().ToString("n");
        var s = new Snapshot(id, DateTimeOffset.UtcNow, _gr.All().ToList());
        _snaps[id] = s;
        return s;
    }
    public bool TryGet(string id, out Snapshot s) => _snaps.TryGetValue(id, out s!);

    public object Diff(string fromId, string toId)
    {
        if (!TryGet(fromId, out var a) || !TryGet(toId, out var b)) return new { added = Array.Empty<object>(), removed = Array.Empty<object>(), changed = Array.Empty<object>() };
        var ka = a.Records.ToDictionary(x => (x.EntityType, x.Key));
        var kb = b.Records.ToDictionary(x => (x.EntityType, x.Key));
        var added = kb.Keys.Except(ka.Keys).Select(k => kb[k]).ToList();
        var removed = ka.Keys.Except(kb.Keys).Select(k => ka[k]).ToList();
        var changed = kb.Keys.Intersect(ka.Keys).Where(k => !Equals(ka[k], kb[k])).Select(k => new { before = ka[k], after = kb[k] }).ToList();
        return new { added, removed, changed };
    }
}


