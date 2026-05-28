using System.Globalization;
using System.IO;
using System.Text;
using Coursach4.Models;

namespace Coursach4.Services;

public sealed class CsvExportService
{
    public (string summaryPath, string eventsPath) Export(string folderPath, IReadOnlyList<KioskMetrics> metrics, IReadOnlyList<SimulationEvent> events, int rejectedByQueue)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var summaryPath = Path.Combine(folderPath, $"summary_{stamp}.csv");
        var eventsPath = Path.Combine(folderPath, $"events_{stamp}.csv");

        File.WriteAllText(summaryPath, BuildSummary(metrics, rejectedByQueue));
        File.WriteAllText(eventsPath, BuildEvents(events));

        return (summaryPath, eventsPath);
    }

    private static string BuildSummary(IReadOnlyList<KioskMetrics> metrics, int rejectedByQueue)
    {
        var sb = new StringBuilder();
        sb.AppendLine("KioskId,SuccessCount,RejectedByVegetables,PeakQueueLength,BusySeconds");
        foreach (var row in metrics)
        {
            sb.AppendLine($"{row.KioskId},{row.SuccessCount},{row.RejectedByVegetablesCount},{row.PeakQueueLength},{row.BusyTime.TotalSeconds:F2}");
        }

        sb.AppendLine();
        sb.AppendLine($"RejectedByQueue,{rejectedByQueue}");
        return sb.ToString();
    }

    private static string BuildEvents(IReadOnlyList<SimulationEvent> events)
    {
        var sb = new StringBuilder();
        sb.AppendLine("UtcTimestamp,SinceStart,KioskId,EventType,Message,Kind");
        foreach (var item in events)
        {
            var kiosk = item.KioskId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            var msg = Escape(item.Message);
            sb.AppendLine($"{item.TimestampUtc:HH:mm:ss.fff},+{item.SinceStart:mm\\:ss},{kiosk},{item.EventType},{msg},{item.Kind}");
        }

        return sb.ToString();
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
