namespace IOC.Optimization.Rules;

using System.Text;

public sealed class RulesEngine
{
    public (double chokePct, double espFreqHz, string rationale) Recommend(
        string liftMethod,
        IEnumerable<(DateTimeOffset ts, double pressurePa, double temperatureC, double flowM3s, double chokePct, double espFreqHz)> window,
        (double minChoke, double maxChoke, double minP, double maxP, double minT, double maxT) c)
    {
        var points = window.OrderByDescending(x => x.ts).Take(60).ToList();
        var last = points.FirstOrDefault();
        var sb = new StringBuilder();
        sb.Append("rules:");

        // Guardrails
        double choke = Math.Clamp(last.chokePct, c.minChoke, c.maxChoke);
        if (last.chokePct != choke) sb.Append(" clamp_choke");

        if (last.pressurePa > c.maxP)
        {
            choke = Math.Max(choke - 1, c.minChoke);
            sb.Append(" reduce_choke_highP");
        }

        if (last.pressurePa < c.minP)
        {
            choke = Math.Min(choke + 1, c.maxChoke);
            sb.Append(" open_choke_lowP");
        }

        double esp = last.espFreqHz;
        if (liftMethod.Equals("ESP", StringComparison.OrdinalIgnoreCase))
        {
            if (last.temperatureC > c.maxT)
            {
                esp = Math.Max(esp - 1, 0);
                sb.Append(" reduce_esp_highT");
            }
            // Temperature within range - keep ESP at current level
        }

        if (points.Count >= 5)
        {
            var recentFlows = points.Take(5).Select(p => p.flowM3s).ToArray();
            if (recentFlows.Zip(recentFlows.Skip(1), (a, b) => b - a).All(d => d > 0))
            {
                sb.Append(" trend_up");
            }
        }

        return (Math.Round(choke, 2), Math.Round(esp, 2), sb.ToString());
    }
}
