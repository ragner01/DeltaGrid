using IOC.Reporting.Models;

namespace IOC.Reporting.Templates;

/// <summary>
/// Template engine for report generation
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// Render a report template with parameters
    /// </summary>
    Task<Models.RenderedReport> RenderAsync(ReportTemplate template, Dictionary<string, object> parameters, CancellationToken ct = default);

    /// <summary>
    /// Validate template syntax
    /// </summary>
    bool ValidateTemplate(string templateContent, out List<string> errors);
}


