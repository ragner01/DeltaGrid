namespace IOC.Emissions;

public enum SourceType { Combustion, Process, Fugitives, Venting, Flaring }
public enum MethodType { DirectMeasurement, EngineeringEstimate, MaterialBalance }

public sealed record FactorDefinition(
    string Code,
    string Pollutant,     // e.g., CO2, CH4, N2O
    string Unit,          // e.g., kg/unit
    string MethodReference,
    string Version,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    double Value,
    double Uncertainty1Sigma // relative (fraction) or absolute depending on Code
);

public sealed record RawReading(
    string Tenant,
    string Site,
    string Asset,
    SourceType Source,
    string Stream,        // e.g., flare, fuel-gas, vent, LDAR
    double Value,
    string Unit,          // engineering unit provided
    double TemperatureC,
    double PressureKPag,
    DateTimeOffset Timestamp
);

public sealed record NormalizedReading(
    RawReading Original,
    double StandardMoles,   // normalized to std conds
    double StandardVolumeM3 // std m3 (if applicable)
);

public sealed record ComputeRequest(
    DateTimeOffset From,
    DateTimeOffset To,
    MethodType Method,
    string? FactorVersion = null
);

public sealed record EmissionPoint(
    DateTimeOffset Timestamp,
    string Pollutant,
    double MassKg,
    double UncertaintyKg
);

public sealed record AggregateResult(
    string Window,
    IReadOnlyList<EmissionPoint> Points,
    Dictionary<string, double> TotalsKg,
    Dictionary<string, (double mean, double p05, double p95)> ConfidenceBands
);

public sealed record LedgerEntry(
    DateTimeOffset Ts,
    string Type,
    string Payload,
    string Hash,
    string? PrevHash
);

public sealed class FactorsStore
{
    private readonly List<FactorDefinition> _factors = new();

    public void AddFactor(FactorDefinition f)
    {
        _factors.Add(f);
    }

    public FactorDefinition? Resolve(string code, DateTimeOffset ts, string? versionOverride = null)
    {
        var query = _factors.Where(x => x.Code == code);
        if (!string.IsNullOrWhiteSpace(versionOverride))
        {
            return query.FirstOrDefault(x => x.Version == versionOverride);
        }
        return query.Where(x => x.EffectiveFrom <= ts && (x.EffectiveTo == null || ts < x.EffectiveTo))
                    .OrderByDescending(x => x.EffectiveFrom)
                    .FirstOrDefault();
    }
}

public sealed class LedgerStore
{
    private readonly List<LedgerEntry> _entries = new();

    public void Append(string type, string payload)
    {
        var prev = _entries.LastOrDefault();
        var prevHash = prev?.Hash;
        var raw = System.Text.Encoding.UTF8.GetBytes($"{prevHash}|{type}|{payload}");
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(raw)).ToLowerInvariant();
        _entries.Add(new LedgerEntry(DateTimeOffset.UtcNow, type, payload, hash, prevHash));
    }

    public IReadOnlyList<LedgerEntry> All() => _entries;
}

public sealed class EmissionsEngine
{
    private const double StdTempK = 273.15 + 15.0;  // 15C
    private const double StdPressPa = 101_325;      // 1 atm

    private readonly FactorsStore _factors;
    private readonly ReadingStore _readings;

    public EmissionsEngine(FactorsStore factors)
    {
        _factors = factors;
        _readings = new ReadingStore();
    }

    public NormalizedReading Normalize(RawReading r)
    {
        // Convert to standard moles using ideal gas approx for gas-like streams.
        // For liquids or LDAR counts, treat Value as already in mass or activity units.
        double tempK = r.TemperatureC + 273.15;
        double absPa = (r.PressureKPag * 1000) + 101_325; // kPag -> Pa (approx)
        double stdFactor = (absPa / StdPressPa) * (StdTempK / tempK);
        double stdVol = r.Unit.Equals("m3", StringComparison.OrdinalIgnoreCase) ? r.Value * stdFactor : 0.0;
        double stdMoles = stdVol > 0 ? stdVol / 0.02479 : 0.0; // 1 mol ~ 24.79 L at 15C ~ 0.02479 m3/mol
        return new NormalizedReading(r, stdMoles, stdVol);
    }

    public AggregateResult Compute(ComputeRequest req)
    {
        var window = _readings.Query(req.From, req.To);
        var points = new List<EmissionPoint>();
        var totals = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in window)
        {
            var norm = Normalize(r);
            // Example mapping: flare stream -> factor code "CO2_FLARE"; fugitives -> "CH4_FUG"
            var code = r.Source switch
            {
                SourceType.Flaring => "CO2_FLARE",
                SourceType.Fugitives => "CH4_FUG",
                SourceType.Combustion => "CO2_COMB",
                SourceType.Venting => "CH4_VENT",
                _ => "CO2_PROC",
            };
            var f = _factors.Resolve(code, r.Timestamp, req.FactorVersion);
            if (f is null) { continue; }
            // Very simplified: mass = activity (std m3) * factor (kg/unit)
            var massKg = norm.StandardVolumeM3 * f.Value;
            var unc = Math.Abs(massKg * f.Uncertainty1Sigma);
            points.Add(new EmissionPoint(r.Timestamp, f.Pollutant, massKg, unc));
            totals[f.Pollutant] = totals.TryGetValue(f.Pollutant, out var t) ? t + massKg : massKg;
        }
        var bands = totals.ToDictionary(k => k.Key, v => (v.Value, v.Value * 0.9, v.Value * 1.1), StringComparer.OrdinalIgnoreCase);
        return new AggregateResult("hourly", points, totals, bands);
    }

    public string GenerateCsv(DateOnly from, DateOnly to)
    {
        // Minimal CSV header compatible with regulatory pack stubs
        return "timestamp,pollutant,mass_kg,uncertainty_kg\n";
    }
}

// In-memory reading store for demo purposes
public sealed class ReadingStore
{
    private readonly List<RawReading> _buffer = new();
    public void Add(RawReading r) => _buffer.Add(r);
    public IReadOnlyList<RawReading> Query(DateTimeOffset from, DateTimeOffset to) => _buffer.Where(r => r.Timestamp >= from && r.Timestamp <= to).ToList();
}


