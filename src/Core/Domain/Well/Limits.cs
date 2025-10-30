namespace IOC.Core.Domain.Well;

public sealed class Limits : IOC.Core.Domain.Base.ValueObject
{
    public double MinChokePct { get; }
    public double MaxChokePct { get; }
    public TimeSpan MinStabilization { get; }

    public Limits(double minChokePct, double maxChokePct, TimeSpan minStabilization)
    {
        if (minChokePct < 0 || maxChokePct > 100 || minChokePct > maxChokePct)
        {
            throw new ArgumentOutOfRangeException(nameof(maxChokePct), "Choke bounds must be within 0-100 and min<=max");
        }
        MinChokePct = minChokePct;
        MaxChokePct = maxChokePct;
        MinStabilization = minStabilization;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MinChokePct;
        yield return MaxChokePct;
        yield return MinStabilization;
    }
}
