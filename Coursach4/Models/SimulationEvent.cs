namespace Coursach4.Models;

public sealed record SimulationEvent(
    DateTime TimestampUtc,
    TimeSpan SinceStart,
    int? KioskId,
    string EventType,
    string Message
);

