using IOC.BuildingBlocks;
using IOC.Core.Domain.Base;

namespace IOC.Core.Domain.Well;

public sealed class Well : AggregateRoot, IHasDomainEvents
{
    private readonly List<IDomainEvent> _events = new();

    public string Name { get; private set; }
    public LiftMethod Lift { get; private set; }
    public Limits Limits { get; private set; }

    public WellState State { get; private set; }
    public DateTimeOffset StateSince { get; private set; }

    public double ChokePercent { get; private set; }

    private Well(string name, LiftMethod lift, Limits limits)
    {
        Name = name;
        Lift = lift;
        Limits = limits;
        State = WellState.ShutIn;
        StateSince = DateTimeOffset.UtcNow;
        ChokePercent = 0;
    }

    public static Well Create(string name, LiftMethod lift, Limits limits)
    {
        var w = new Well(name, lift, limits);
        return w;
    }

    public Result SetChoke(double newPercent)
    {
        if (newPercent < Limits.MinChokePct || newPercent > Limits.MaxChokePct)
        {
            return Result.Failure($"Choke {newPercent}% outside bounds {Limits.MinChokePct}-{Limits.MaxChokePct}%");
        }
        if (Math.Abs(newPercent - ChokePercent) < 0.0001)
        {
            return Result.Success();
        }
        ChokePercent = newPercent;
        _events.Add(new ChokeAdjusted(Id, newPercent));
        return Result.Success();
    }

    public Result ChangeState(WellState target, DateTimeOffset now)
    {
        if (!IsLegalTransition(State, target))
        {
            return Result.Failure($"Illegal transition {State} -> {target}");
        }
        if (now - StateSince < Limits.MinStabilization && RequiresStability(State, target))
        {
            return Result.Failure($"State must stabilize for {Limits.MinStabilization}");
        }
        State = target;
        StateSince = now;
        _events.Add(new WellStateChanged(Id, target, now));
        return Result.Success();
    }

    private static bool IsLegalTransition(WellState from, WellState to)
    {
        if (from == to) return true;
        return (from, to) switch
        {
            (WellState.ShutIn, WellState.RampUp) => true,
            (WellState.RampUp, WellState.Stable) => true,
            (WellState.Stable, WellState.Decline) => true,
            (WellState.Stable, WellState.Trip) => true,
            (WellState.Trip, WellState.Maintenance) => true,
            (WellState.Maintenance, WellState.RampUp) => true,
            (_, WellState.Alarm) => true,
            (WellState.Decline, WellState.Stable) => true,
            _ => false
        };
    }

    private static bool RequiresStability(WellState from, WellState to)
    {
        return (from, to) switch
        {
            (WellState.RampUp, WellState.Stable) => true,
            (WellState.Stable, WellState.Decline) => true,
            _ => false
        };
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events;
    public void ClearDomainEvents() => _events.Clear();
}

public sealed record WellStateChanged(Guid WellId, WellState NewState, DateTimeOffset ChangedAt) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public sealed record ChokeAdjusted(Guid WellId, double NewPercent) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public sealed record LimitBreach(Guid WellId, string Reason) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
