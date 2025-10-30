namespace FieldApp.Forms;

public enum FieldType { Text, Number, DateTime, Checkbox, Signature, Photo }

public sealed record FormField(string Id, string Label, FieldType Type, bool Required);
public sealed record FormSchema(string Id, string Title, IReadOnlyList<FormField> Fields);

public sealed class FormInstance
{
    public string SchemaId { get; }
    public Dictionary<string, object?> Values { get; } = new();
    public FormInstance(string schemaId) { SchemaId = schemaId; }
}

public static class FormRenderer
{
    public static FormInstance CreateInstance(FormSchema schema)
    {
        var inst = new FormInstance(schema.Id);
        foreach (var f in schema.Fields) inst.Values[f.Id] = null;
        return inst;
    }

    public static bool Validate(FormSchema schema, FormInstance instance, out string error)
    {
        foreach (var f in schema.Fields)
        {
            if (f.Required && (instance.Values.TryGetValue(f.Id, out var v) == false || v is null || (v is string s && string.IsNullOrWhiteSpace(s))))
            {
                error = $"Field '{f.Label}' is required"; return false;
            }
        }
        error = string.Empty; return true;
    }
}
