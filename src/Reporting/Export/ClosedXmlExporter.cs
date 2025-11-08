using IOC.Reporting.Models;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using IOC.Reporting.Templates;
using System.Text.Json;

namespace IOC.Reporting.Export;

/// <summary>
/// ClosedXML implementation of Excel exporter
/// </summary>
public sealed class ClosedXmlExporter : IExcelExporter
{
    private readonly ILogger<ClosedXmlExporter> _logger;

    public ClosedXmlExporter(ILogger<ClosedXmlExporter> logger)
    {
        _logger = logger;
    }

    public Task<byte[]> ExportAsync(Models.RenderedReport report, CancellationToken ct = default)
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Report");

            var jsonData = ReportDataHelper.Normalize(report.Data);

            // Header row
            worksheet.Cell(1, 1).Value = "DeltaGrid Report";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;

            worksheet.Cell(2, 1).Value = $"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC";

            // Render data sections (simplified; extend for complex templates)
            int row = 4;
            if (jsonData.TryGetProperty("sections", out var sections) && sections.ValueKind == JsonValueKind.Array)
            {
                foreach (var section in sections.EnumerateArray())
                {
                    var sectionTitle = section.TryGetProperty("title", out var st) ? st.GetString() : null;
                    if (!string.IsNullOrEmpty(sectionTitle))
                    {
                        worksheet.Cell(row, 1).Value = sectionTitle;
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        row++;
                    }

                    if (section.TryGetProperty("tables", out var tables) && tables.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var table in tables.EnumerateArray())
                        {
                            if (table.TryGetProperty("headers", out var headers) && table.TryGetProperty("rows", out var rows))
                            {
                                int col = 1;
                                foreach (var header in headers.EnumerateArray())
                                {
                                    worksheet.Cell(row, col).Value = header.GetString();
                                    worksheet.Cell(row, col).Style.Font.Bold = true;
                                    col++;
                                }
                                row++;

                                foreach (var dataRow in rows.EnumerateArray())
                                {
                                    col = 1;
                                    foreach (var cell in dataRow.EnumerateArray())
                                    {
                                        worksheet.Cell(row, col).Value = cell.GetString();
                                        col++;
                                    }
                                    row++;
                                }
                                row++; // Blank row between tables
                            }
                        }
                    }
                }
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var excelBytes = stream.ToArray();

            _logger.LogInformation("Generated Excel report: {Size} bytes", excelBytes.Length);

            return Task.FromResult(excelBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Excel");
            throw;
        }
    }
}

