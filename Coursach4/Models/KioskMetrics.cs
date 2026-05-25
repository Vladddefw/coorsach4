namespace Coursach4.Models;

public sealed class KioskMetrics
{
    public int KioskId { get; init; }
    public int SuccessCount { get; set; }
    public int RejectedByVegetablesCount { get; set; }
    public int PeakQueueLength { get; set; }
    public TimeSpan BusyTime { get; set; }
    public double UtilizationPercent(double totalSeconds)
    {
        if (totalSeconds <= 0)
        {
            return 0;
        }

        return BusyTime.TotalSeconds / totalSeconds * 100.0;
    }
}

