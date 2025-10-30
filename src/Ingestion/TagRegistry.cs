using System.Collections.Concurrent;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IOC.Ingestion;

public sealed class TagDefinition
{
    public string TagId { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty; // ISO unit
    public double? Deadband { get; set; }
    public double? ScaleFactor { get; set; }
    public double? ScaleOffset { get; set; }
}

public sealed class TagRegistry
{
    private readonly ConcurrentDictionary<string, TagDefinition> _byId = new();
    private readonly string _path;

    public TagRegistry(IConfiguration config)
    {
        _path = config.GetValue<string>("Ingestion:TagConfigPath") ?? "config/tags.yaml";
        Load();
    }

    public bool TryGet(string tagId, out TagDefinition def) => _byId.TryGetValue(tagId, out def!);

    public void Load()
    {
        if (!File.Exists(_path)) return;
        var yaml = File.ReadAllText(_path, Encoding.UTF8);
        var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
        var defs = deserializer.Deserialize<List<TagDefinition>>(yaml) ?? [];
        _byId.Clear();
        foreach (var d in defs)
        {
            if (string.IsNullOrWhiteSpace(d.TagId)) continue;
            _byId[d.TagId] = d;
        }
    }
}

public sealed class TagRegistryReloader : BackgroundService
{
    private readonly TagRegistry _registry;
    private readonly ILogger<TagRegistryReloader> _logger;
    private readonly string _path;

    public TagRegistryReloader(TagRegistry registry, IConfiguration config, ILogger<TagRegistryReloader> logger)
    {
        _registry = registry;
        _logger = logger;
        _path = config.GetValue<string>("Ingestion:TagConfigPath") ?? "config/tags.yaml";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTime last = DateTime.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (File.Exists(_path))
                {
                    var ts = File.GetLastWriteTimeUtc(_path);
                    if (ts > last)
                    {
                        _registry.Load();
                        last = ts;
                        _logger.LogInformation("Tag registry reloaded at {ts}", ts);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading tag registry");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
