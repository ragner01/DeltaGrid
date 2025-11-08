using System.Text.Json;
using IOC.Reporting.Models;

namespace IOC.Reporting.Templates;

/// <summary>
/// JSON-based template engine (simple parameter substitution)
/// </summary>
public sealed class JsonTemplateEngine : ITemplateEngine
{
    private readonly ILogger<JsonTemplateEngine> _logger;

    public JsonTemplateEngine(ILogger<JsonTemplateEngine> logger)
    {
        _logger = logger;
    }

    public Task<Models.RenderedReport> RenderAsync(ReportTemplate template, Dictionary<string, object> parameters, CancellationToken ct = default)
    {
        try
        {
            // Simple parameter substitution in JSON template
            var templateJson = template.TemplateContent;
            foreach (var param in parameters)
            {
                templateJson = templateJson.Replace($"{{{{{param.Key}}}}}", param.Value.ToString() ?? string.Empty);
                templateJson = templateJson.Replace($"{{{{params.{param.Key}}}}}", JsonSerializer.Serialize(param.Value));
            }

            var data = JsonSerializer.Deserialize<JsonElement>(templateJson);
            var metadata = new Dictionary<string, object>
            {
                ["templateId"] = template.Id,
                ["templateVersion"] = template.Version,
                ["reportType"] = template.Type.ToString(),
                ["renderedAt"] = DateTimeOffset.UtcNow
            };

            return Task.FromResult(new Models.RenderedReport
            {
                TemplateId = template.Id,
                Type = template.Type,
                GeneratedAt = DateTimeOffset.UtcNow,
                Content = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data)),
                ContentType = "application/json",
                Data = data,
                Metadata = metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template {TemplateId}", template.Id);
            throw;
        }
    }

    public bool ValidateTemplate(string templateContent, out List<string> errors)
    {
        errors = new List<string>();
        try
        {
            JsonSerializer.Deserialize<JsonElement>(templateContent);
            return true;
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON: {ex.Message}");
            return false;
        }
    }
}


