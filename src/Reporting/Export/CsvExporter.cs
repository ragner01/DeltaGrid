using IOC.Reporting.Models;
using Microsoft.Extensions.Logging;
using IOC.Reporting.Templates;
using System.Text;
using System.Text.Json;

namespace IOC.Reporting.Export;

/// <summary>
/// CSV exporter for reports
/// </summary>
public sealed class CsvExporter : ICsvExporter
{
    private readonly ILogger<CsvExporter> _logger;

    public CsvExporter(ILogger<CsvExporter> logger)
    {
        _logger = logger;
    }

    public Task<byte[]> ExportAsync(Models.RenderedReport report, CancellationToken ct = default)
    {
        try
        {
            var csv = new StringBuilder();
            var jsonData = ReportDataHelper.Normalize(report.Data);

            // Render data sections (simplified; extend for complex templates)
            if (jsonData.TryGetProperty("sections", out var sections) && sections.ValueKind == JsonValueKind.Array)
            {
                foreach (var section in sections.EnumerateArray())
                {
                    if (section.TryGetProperty("tables", out var tables) && tables.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var table in tables.EnumerateArray())
                        {
                            if (table.TryGetProperty("headers", out var headers) && table.TryGetProperty("rows", out var rows))
                            {
                                // Headers
                                csv.AppendLine(string.Join(",", headers.EnumerateArray().Select(h => EscapeCsv(h.GetString() ?? string.Empty))));

                                // Rows
                                foreach (var dataRow in rows.EnumerateArray())
                                {
                                    csv.AppendLine(string.Join(",", dataRow.EnumerateArray().Select(c => EscapeCsv(c.GetString() ?? string.Empty))));
                                }
                            }
                        }
                    }
                }
            }

            var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
            _logger.LogInformation("Generated CSV report: {Size} bytes", csvBytes.Length);

            return Task.FromResult(csvBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export CSV");
            throw;
        }
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
