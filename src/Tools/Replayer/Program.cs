using System.Text.Json;
using IOC.Ingestion;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: replayer <path-to-jsonl>");
    return;
}

var path = args[0];
if (!File.Exists(path))
{
    Console.Error.WriteLine($"File not found: {path}");
    return;
}

var builder = Host.CreateApplicationBuilder(args);
var publisher = new EventHubsPublisher(builder.Configuration);

var batch = new List<IngestionEnvelope>(1000);
await foreach (var line in ReadLinesAsync(path))
{
    if (string.IsNullOrWhiteSpace(line)) continue;
    var env = JsonSerializer.Deserialize<IngestionEnvelope>(line);
    if (env is null) continue;
    batch.Add(env);
    if (batch.Count >= 1000)
    {
        await publisher.PublishBatchAsync(batch, CancellationToken.None);
        batch.Clear();
    }
}

if (batch.Count > 0)
{
    await publisher.PublishBatchAsync(batch, CancellationToken.None);
}

static async IAsyncEnumerable<string> ReadLinesAsync(string file)
{
    using var sr = new StreamReader(file);
    string? line;
    while ((line = await sr.ReadLineAsync()) is not null)
    {
        yield return line;
    }
}
