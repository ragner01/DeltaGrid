using IOC.Reporting.Models;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IOC.Reporting.Templates;
using System.Text.Json;

namespace IOC.Reporting.Export;

/// <summary>
/// QuestPDF implementation of PDF exporter with watermarking
/// </summary>
public sealed class QuestPdfExporter : IPdfExporter
{
    private readonly ILogger<QuestPdfExporter> _logger;

    public QuestPdfExporter(ILogger<QuestPdfExporter> logger)
    {
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community; // Use community license; configure for production
    }

    public Task<byte[]> ExportAsync(Models.RenderedReport report, string? watermark = null, CancellationToken ct = default)
    {
        try
        {
            var jsonData = ReportDataHelper.Normalize(report.Data);
            var title = jsonData.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "Report" : "Report";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    // Watermark
                    if (!string.IsNullOrEmpty(watermark))
                    {
                        page.Background()
                            .AlignCenter()
                            .AlignMiddle()
                            .Rotate(-45)
                            .Text(watermark)
                            .FontSize(48)
                            .FontColor(Colors.Grey.Lighten2);
                    }

                    // Header
                    page.Header()
                        .Text(title)
                        .FontSize(20)
                        .Bold();

                    // Content
                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Item().Text($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
                            column.Item().PaddingTop(1, Unit.Centimetre);

                            // Render data sections (simplified; extend for complex templates)
                            if (jsonData.TryGetProperty("sections", out var sections) && sections.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var section in sections.EnumerateArray())
                                {
                                    var sectionTitle = section.TryGetProperty("title", out var st) ? st.GetString() : null;
                                    if (!string.IsNullOrEmpty(sectionTitle))
                                    {
                                        column.Item().Text(sectionTitle).FontSize(14).Bold();
                                        column.Item().PaddingTop(0.5f, Unit.Centimetre);
                                    }

                                    if (section.TryGetProperty("content", out var content))
                                    {
                                        column.Item().Text(content.GetString() ?? string.Empty);
                                    }
                                }
                            }
                        });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                });
            });

            var pdfBytes = document.GeneratePdf();
            _logger.LogInformation("Generated PDF report: {Size} bytes", pdfBytes.Length);

            return Task.FromResult(pdfBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export PDF");
            throw;
        }
    }
}

