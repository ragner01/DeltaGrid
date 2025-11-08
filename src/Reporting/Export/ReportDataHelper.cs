using System.Text.Json;

namespace IOC.Reporting.Export;

/// <summary>
/// Utility helpers for normalizing report payloads into JsonElement instances.
/// </summary>
internal static class ReportDataHelper
{
    public static JsonElement Normalize(object? data)
    {
        try
        {
            return data switch
            {
                JsonElement element => element.ValueKind == JsonValueKind.Undefined
                    ? JsonSerializer.Deserialize<JsonElement>("{}")
                    : element.Clone(),
                JsonDocument doc => doc.RootElement.Clone(),
                string json when !string.IsNullOrWhiteSpace(json) => JsonSerializer.Deserialize<JsonElement>(json),
                byte[] bytes when bytes.Length > 0 => JsonSerializer.Deserialize<JsonElement>(bytes),
                _ => JsonSerializer.SerializeToElement(data ?? new { })
            };
        }
        catch
        {
            return JsonSerializer.Deserialize<JsonElement>("{}");
        }
    }
}
