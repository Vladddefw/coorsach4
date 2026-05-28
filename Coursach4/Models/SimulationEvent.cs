namespace Coursach4.Models;

public enum SimulationEventKind
{
    Neutral,
    Success,
    Rejected
}

public sealed record SimulationEvent(
    DateTime TimestampUtc,
    TimeSpan SinceStart,
    int? KioskId,
    string EventType,
    string Message,
    SimulationEventKind Kind
);
