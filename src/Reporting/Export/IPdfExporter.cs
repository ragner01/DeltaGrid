using IOC.Reporting.Models;

namespace IOC.Reporting.Export;

/// <summary>
/// PDF exporter for reports
/// </summary>
public interface IPdfExporter
{
    /// <summary>
    /// Export rendered report to PDF with optional watermark
    /// </summary>
    Task<byte[]> ExportAsync(Models.RenderedReport report, string? watermark = null, CancellationToken ct = default);
}

/// <summary>
/// Excel exporter for reports
/// </summary>
public interface IExcelExporter
{
    /// <summary>
    /// Export rendered report to Excel
    /// </summary>
    Task<byte[]> ExportAsync(RenderedReport report, CancellationToken ct = default);
}

/// <summary>
/// CSV exporter for reports
/// </summary>
public interface ICsvExporter
{
    /// <summary>
    /// Export rendered report to CSV
    /// </summary>
    Task<byte[]> ExportAsync(RenderedReport report, CancellationToken ct = default);
}


