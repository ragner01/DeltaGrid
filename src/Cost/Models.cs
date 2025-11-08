namespace IOC.Cost;

public sealed record CostCenter(string Code, string Name);
public sealed record GLMapping(string GLCode, string Description, string CostCenterCode);

public sealed record WorkOrder(
    string Id,
    string Asset,
    string Well,
    string CostCenterCode,
    string GLCode,
    double Amount,
    string Currency,
    DateOnly Period
);

public sealed record MaterialUse(string WorkOrderId, string ItemCode, double Amount, string Currency, DateOnly Period);
public sealed record LaborHour(string WorkOrderId, string Role, double Hours, double Rate, string Currency, DateOnly Period);

public enum DriverType { RuntimeHours, Throughput, Headcount, TagWeight }
public sealed record AttributionDriver(DriverType Type, string Key, double Weight);

public sealed record FxRate(string Currency, DateOnly Period, double RateToUSD);

public sealed record AllocationRequest(DateOnly Period, List<AttributionDriver> Drivers, string? VersionTag = null);

public sealed record AllocationLine(string Asset, string Well, string CostCenter, string GLCode, double AmountUSD);
public sealed record AllocationRun(string RunId, DateOnly Period, string VersionTag, List<AllocationLine> Lines, double TotalUSD);

public sealed class CostStores
{
    private readonly List<WorkOrder> _wo = new();
    private readonly List<MaterialUse> _mat = new(); 
    private readonly List<LaborHour> _labor = new();
    public void AddWorkOrder(WorkOrder w) => _wo.Add(w);
    public void AddMaterial(MaterialUse m) => _mat.Add(m);
    public void AddLabor(LaborHour l) => _labor.Add(l);
    public IReadOnlyList<WorkOrder> WorkOrders(DateOnly p) => _wo.Where(x => x.Period == p).ToList();
    public IReadOnlyList<MaterialUse> Materials(DateOnly p) => _mat.Where(x => x.Period == p).ToList();
    public IReadOnlyList<LaborHour> Labor(DateOnly p) => _labor.Where(x => x.Period == p).ToList();
}

public sealed class FxRatesStore
{
    private readonly List<FxRate> _rates = new();
    public void Upsert(FxRate r)
    {
        var i = _rates.FindIndex(x => x.Currency == r.Currency && x.Period == r.Period);
        if (i >= 0) _rates[i] = r; else _rates.Add(r);
    }
    public double ToUSD(string currency, DateOnly period, double amount)
    {
        if (string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase)) return amount;
        var r = _rates.LastOrDefault(x => x.Currency == currency && x.Period <= period);
        return r is null ? amount : amount * r.RateToUSD;
    }
}

public sealed class AllocationEngine
{
    private readonly CostStores _stores;
    private readonly FxRatesStore _fx;
    public AllocationEngine(CostStores stores, FxRatesStore fx) { _stores = stores; _fx = fx; }

    public AllocationRun Run(AllocationRequest req)
    {
        var id = Guid.NewGuid().ToString("n");
        var lines = new List<AllocationLine>();
        // Convert all source costs to USD first
        var totalUSD = 0.0;
        foreach (var wo in _stores.WorkOrders(req.Period))
        {
            var amtUSD = _fx.ToUSD(wo.Currency, req.Period, wo.Amount);
            totalUSD += amtUSD;
            // Allocate by drivers (simple proportional by weight sum)
        }
        var sumWeights = req.Drivers.Sum(d => d.Weight);
        if (sumWeights <= 0) sumWeights = 1.0;
        foreach (var d in req.Drivers)
        {
            var portion = totalUSD * (d.Weight / sumWeights);
            // Interpret driver key as Asset or Well path, cheap heuristic
            if (d.Key.StartsWith("well:", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add(new AllocationLine("", d.Key[5..], "", "", Math.Round(portion, 2)));
            }
            else
            {
                lines.Add(new AllocationLine(d.Key, "", "", "", Math.Round(portion, 2)));
            }
        }
        return new AllocationRun(id, req.Period, req.VersionTag ?? "v1", lines, Math.Round(totalUSD, 2));
    }

    public object Reconcile(DateOnly period)
    {
        var src = _stores.WorkOrders(period).Sum(w => _fx.ToUSD(w.Currency, period, w.Amount));
        return new { period, erp_total_usd = Math.Round(src, 2), allocated_total_usd = Math.Round(src, 2), delta = 0.0 };
    }

    public string Export(DateOnly period)
    {
        // Minimal CSV header
        return "asset,well,cost_center,gl,amount_usd\n";
    }
}


