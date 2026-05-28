using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Coursach4.Models;

namespace Coursach4.Services;

public sealed class HtmlExportService
{
    public (string htmlPath, string jsonPath) Export(string folderPath, IReadOnlyList<KioskMetrics> metrics, IReadOnlyList<SimulationEvent> events, int rejectedByQueue)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var htmlPath = Path.Combine(folderPath, $"report_{stamp}.html");
        var jsonPath = Path.Combine(folderPath, $"report_{stamp}.json");

        File.WriteAllText(htmlPath, BuildHtml(metrics, events, rejectedByQueue), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(jsonPath, BuildJson(metrics, events, rejectedByQueue), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return (htmlPath, jsonPath);
    }

    private static string BuildJson(IReadOnlyList<KioskMetrics> metrics, IReadOnlyList<SimulationEvent> events, int rejectedByQueue)
    {
        var payload = new
        {
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            rejectedByQueue,
            kiosks = metrics.Select(row => new
            {
                row.KioskId,
                row.SuccessCount,
                row.RejectedByVegetablesCount,
                row.PeakQueueLength,
                busySeconds = Math.Round(row.BusyTime.TotalSeconds, 2)
            }),
            events = events.Select(item => new
            {
                timestampUtc = item.TimestampUtc.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                sinceStart = item.SinceStart.ToString("mm\\:ss", CultureInfo.InvariantCulture),
                kioskId = item.KioskId,
                eventType = item.EventType,
                message = item.Message,
                kind = item.Kind.ToString()
            })
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(payload, options);
    }

    private static string BuildHtml(IReadOnlyList<KioskMetrics> metrics, IReadOnlyList<SimulationEvent> events, int rejectedByQueue)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"uk\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">" );
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" );
        sb.AppendLine("<title>Звіт симуляції</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:20px;color:#222}");
        sb.AppendLine("h1,h2{margin:0 0 12px 0}");
        sb.AppendLine("table{border-collapse:collapse;width:100%;margin:8px 0 20px 0}");
        sb.AppendLine("th,td{border:1px solid #ddd;padding:8px;text-align:left}");
        sb.AppendLine("th{background:#f3f3f3}");
        sb.AppendLine(".meta{margin:0 0 12px 0;color:#555}");
        sb.AppendLine(".success{background:#ddf3dd}");
        sb.AppendLine(".rejected{background:#f9d6d5}");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<h1>Звіт симуляції</h1>");
        sb.AppendLine($"<div class=\"meta\">Створено: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>");

        sb.AppendLine("<h2>Підсумок по кіосках</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Кіоск</th><th>Успішно</th><th>Відмов через овочі</th><th>Пікова черга</th><th>Зайнятість (с)</th></tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var row in metrics)
        {
            sb.AppendLine($"<tr><td>{row.KioskId}</td><td>{row.SuccessCount}</td><td>{row.RejectedByVegetablesCount}</td><td>{row.PeakQueueLength}</td><td>{row.BusyTime.TotalSeconds:F2}</td></tr>");
        }
        sb.AppendLine($"<tr><td colspan=\"4\"><strong>Відмов через чергу</strong></td><td><strong>{rejectedByQueue}</strong></td></tr>");
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<h2>Події</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>UTC</th><th>Від старту</th><th>Кіоск</th><th>Подія</th><th>Повідомлення</th><th>Тип</th></tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var item in events)
        {
            var kiosk = item.KioskId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            var rowClass = item.Kind switch
            {
                SimulationEventKind.Success => "success",
                SimulationEventKind.Rejected => "rejected",
                _ => string.Empty
            };
            sb.AppendLine($"<tr class=\"{rowClass}\"><td>{item.TimestampUtc:HH:mm:ss.fff}</td><td>+{item.SinceStart:mm\\:ss}</td><td>{kiosk}</td><td>{EscapeHtml(item.EventType)}</td><td>{EscapeHtml(item.Message)}</td><td>{item.Kind}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string EscapeHtml(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
