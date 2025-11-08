using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using MQTTnet;
using MQTTnet.Client;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Microsoft.Extensions.Configuration;

namespace IOC.Ingestion;

public sealed class OpcUaConnector : IConnector
{
    private readonly string _endpoint;
    private readonly string _site;
    private readonly string _asset;
    private readonly string[] _nodes;
    private Session? _session;

    public OpcUaConnector(IConfiguration cfg)
    {
        _endpoint = cfg["Connectors:OpcUa:EndpointUrl"] ?? "opc.tcp://localhost:4840";
        _site = cfg["Connectors:OpcUa:SiteId"] ?? "site-1";
        _asset = cfg["Connectors:OpcUa:AssetId"] ?? "asset-1";
        _nodes = (cfg.GetSection("Connectors:OpcUa:NodeIds").Get<string[]>() ?? new[] {"ns=2;s=well1.tubing_pressure"});
    }

    public string Name => "OPC-UA";

    public async IAsyncEnumerable<TagReading> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var application = new ApplicationInstance { ApplicationName = "IOC.Ingestion", ApplicationType = ApplicationType.Client };
        await application.LoadApplicationConfiguration(false);
        await application.CheckApplicationInstanceCertificate(false, 0);

        var endpointDescription = CoreClientUtils.SelectEndpoint(_endpoint, true);
        var endpointConfiguration = EndpointConfiguration.Create(application.ApplicationConfiguration);
        var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);
        _session = await Session.Create(application.ApplicationConfiguration, endpoint, false, "IOC", 60000, new UserIdentity(new AnonymousIdentityToken()), null);

        var subscription = new Subscription(_session.DefaultSubscription) { PublishingInterval = 250 };
        var monitored = new List<MonitoredItem>();
        foreach (var id in _nodes)
        {
            var mi = new MonitoredItem(subscription.DefaultItem)
            {
                StartNodeId = new NodeId(id),
                AttributeId = Attributes.Value,
                SamplingInterval = 250,
                QueueSize = 10,
                DiscardOldest = true
            };
            monitored.Add(mi);
        }
        subscription.AddItems(monitored);
        _session.AddSubscription(subscription);
        subscription.Create();

        var channel = Channel.CreateUnbounded<TagReading>();
        foreach (var mi in monitored)
        {
            mi.Notification += (item, args) =>
            {
                void Handle(MonitoredItemNotification notification)
                {
                    var dv = notification.Value;
                    if (dv.Value is IConvertible convertible)
                    {
                        var tv = Convert.ToDouble(convertible);
                        var q = dv.StatusCode == StatusCodes.Good ? "Good" : "Bad";
                        var timestamp = dv.SourceTimestamp != default(DateTime) ? DateTimeOffset.FromFileTime(dv.SourceTimestamp.ToFileTime()) : DateTimeOffset.UtcNow;
                        channel.Writer.TryWrite(new TagReading($"opc:{mi.StartNodeId}", tv, q, timestamp, string.Empty, _site, _asset));
                    }
                }

                if (args.NotificationValue is MonitoredItemNotification single)
                {
                    Handle(single);
                }
                else if (args.NotificationValue is Opc.Ua.ExtensionObject extObj && extObj.Body is MonitoredItemNotification[] notifications)
                {
                    foreach (var note in notifications)
                    {
                        Handle(note);
                    }
                }
            };
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            while (channel.Reader.TryRead(out var r))
            {
                yield return r;
            }
            await Task.Delay(50, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is not null)
        {
            await Task.Run(() => _session.Close());
            _session.Dispose();
        }
    }
}

public sealed class MqttConnector : IConnector
{
    private readonly string _broker;
    private readonly string _topic;
    private readonly string _site;
    private readonly string _asset;
    private IMqttClient? _client;

    public MqttConnector(IConfiguration cfg)
    {
        _broker = cfg["Connectors:Mqtt:Broker"] ?? "mqtt://localhost:1883";
        _topic = cfg["Connectors:Mqtt:Topic"] ?? "ioc/+/+";
        _site = cfg["Connectors:Mqtt:SiteId"] ?? "site-1";
        _asset = cfg["Connectors:Mqtt:AssetId"] ?? "asset-1";
    }

    public string Name => "MQTT";

    public async IAsyncEnumerable<TagReading> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();
        var opts = new MqttClientOptionsBuilder()
            .WithClientId($"ioc-{Guid.NewGuid():N}")
            .WithTcpServer(new Uri(_broker).Host, new Uri(_broker).IsDefaultPort ? 1883 : new Uri(_broker).Port)
            .WithCleanSession()
            .Build();

        var channel = Channel.CreateUnbounded<TagReading>();

        _client.ApplicationMessageReceivedAsync += e =>
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                if (double.TryParse(payload, out var v))
                {
                    var tagId = $"mqtt:{topic}";
                    channel.Writer.TryWrite(new TagReading(tagId, v, "Good", DateTimeOffset.UtcNow, "", _site, _asset));
                }
            }
            catch { }
            return Task.CompletedTask;
        };

        await _client.ConnectAsync(opts, cancellationToken);
        await _client.SubscribeAsync(_topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            while (channel.Reader.TryRead(out var r))
            {
                yield return r;
            }
            await Task.Delay(50, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null) await _client.DisconnectAsync();
    }
}

public sealed class PiConnector : IConnector
{
    private readonly HttpClient _http = new();
    private readonly string _baseUrl;
    private readonly string _tagPath;
    private readonly string _site;
    private readonly string _asset;

    public PiConnector(IConfiguration cfg)
    {
        _baseUrl = cfg["Connectors:Pi:BaseUrl"] ?? "https://localhost/piwebapi";
        _tagPath = cfg["Connectors:Pi:TagPath"] ?? "/streams/{webid}/value";
        _site = cfg["Connectors:Pi:SiteId"] ?? "site-1";
        _asset = cfg["Connectors:Pi:AssetId"] ?? "asset-1";
        var token = cfg["Connectors:Pi:BearerToken"];
        if (!string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public string Name => "PI";

    public async IAsyncEnumerable<TagReading> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Simple poller example, replace with stream if available
        while (!cancellationToken.IsCancellationRequested)
        {
            TagReading? reading = null;
            try
            {
                var resp = await _http.GetAsync(_baseUrl + _tagPath, cancellationToken);
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync(cancellationToken);
                    if (double.TryParse(System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("Value").ToString(), out var v))
                    {
                        reading = new TagReading("pi:tag", v, "Good", DateTimeOffset.UtcNow, string.Empty, _site, _asset);
                    }
                }
            }
            catch
            {
                // swallow transient errors in simulator
            }

            if (reading is not null)
            {
                yield return reading;
            }

            await Task.Delay(300, cancellationToken);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
