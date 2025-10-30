using Microsoft.AspNetCore.SignalR.Client;

namespace OpsConsole.Services;

public sealed class OpsHubClient : IAsyncDisposable
{
    private readonly HubConnection _conn;
    public event Action<string>? OnMessage;

    public OpsHubClient(string baseUrl)
    {
        _conn = new HubConnectionBuilder()
            .WithUrl(new Uri(new Uri(baseUrl), "/opsHub"))
            .WithAutomaticReconnect()
            .Build();
        _conn.On<string>("msg", m => OnMessage?.Invoke(m));
    }

    public Task StartAsync() => _conn.StartAsync();
    public Task StopAsync() => _conn.StopAsync();
    public async ValueTask DisposeAsync() { await _conn.DisposeAsync(); }
}
