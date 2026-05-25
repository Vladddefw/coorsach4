namespace Coursach4.Models;

public sealed class SimulationConfig
{
    public int KioskCount { get; init; } = 4;
    public int QueueLimit { get; init; } = 3;
    public int CutMinutes { get; init; } = 3;
    public int ServiceMinMinutes { get; init; } = 6;
    public int ServiceMaxMinutes { get; init; } = 7;
    public int InitialVegetablePortions { get; init; } = 4;
    public int FanArrivalMinMinutes { get; init; } = 1;
    public int FanArrivalMaxMinutes { get; init; } = 3;
    public int TimeScaleMsPerMinute { get; init; } = 6_000;
}

