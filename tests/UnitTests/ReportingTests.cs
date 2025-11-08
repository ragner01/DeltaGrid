using IOC.Reporting.Models;
using IOC.Reporting.Templates;
using IOC.Reporting.Export;
using IOC.Reporting.Services;
using IOC.Reporting.Persistence;
using Xunit;
using System.Threading.Tasks;

namespace IOC.UnitTests;

public class ReportTemplateEngineTests
{
    [Fact]
    public void RenderAsync_SimpleTemplate_ReplacesParameters()
    {
        var engine = new JsonTemplateEngine(new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<JsonTemplateEngine>());
        var template = new ReportTemplate
        {
            Id = "test-1",
            Name = "Test Template",
            Type = ReportType.DailyProduction,
            Version = "1.0",
            TemplateContent = """{"title": "{{title}}", "date": "{{date}}"}""",
            Parameters = new List<string> { "title", "date" }
        };

        var parameters = new Dictionary<string, object>
        {
            ["title"] = "Daily Production",
            ["date"] = "2025-10-30"
        };

        var rendered = engine.RenderAsync(template, parameters).Result;

        Assert.NotNull(rendered);
        Assert.NotNull(rendered.Data);
    }

    [Fact]
    public void ValidateTemplate_ValidJson_ReturnsTrue()
    {
        var engine = new JsonTemplateEngine(new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<JsonTemplateEngine>());
        var isValid = engine.ValidateTemplate("""{"title": "Test"}""", out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateTemplate_InvalidJson_ReturnsFalse()
    {
        var engine = new JsonTemplateEngine(new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<JsonTemplateEngine>());
        var isValid = engine.ValidateTemplate("{invalid json", out var errors);

        Assert.False(isValid);
        Assert.NotEmpty(errors);
    }
}

public class ReportServiceTests
{
    [Fact]
    public async Task GenerateAsync_ValidRequest_CreatesReport()
    {
        var repo = new InMemoryReportRepository();
        repo.SeedTemplates();

        var templateEngine = new JsonTemplateEngine(new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<JsonTemplateEngine>());
        var pdfExporter = new QuestPdfExporter(new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<QuestPdfExporter>());
        var excelExporter = new ClosedXmlExporter(new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<ClosedXmlExporter>());
        var csvExporter = new CsvExporter(new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<CsvExporter>());
        var logger = new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<ReportService>();

        var service = new ReportService(templateEngine, pdfExporter, excelExporter, csvExporter, repo, logger);

        var request = new ReportRequest
        {
            TemplateId = "daily-prod-1",
            TenantId = "tenant-1",
            Parameters = new Dictionary<string, object>
            {
                ["date"] = "2025-10-30",
                ["siteId"] = "site-1"
            },
            Format = ReportFormat.PDF
        };

        var report = await service.GenerateAsync(request);

        Assert.NotNull(report);
        Assert.Equal("daily-prod-1", report.TemplateId);
        Assert.Equal("tenant-1", report.TenantId);
        Assert.True(report.Content.Length > 0);
    }
}


