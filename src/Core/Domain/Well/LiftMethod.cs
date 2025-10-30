namespace IOC.Core.Domain.Well;

public enum LiftMethod
{
    Natural = 0,
    GasLift = 1,
    ESP = 2
}

public enum WellState
{
    ShutIn = 0,
    RampUp = 1,
    Stable = 2,
    Decline = 3,
    Trip = 4,
    Alarm = 5,
    Maintenance = 6
}
