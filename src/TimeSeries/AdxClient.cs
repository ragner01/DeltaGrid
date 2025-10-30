using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;

namespace IOC.TimeSeries;

public sealed class AdxClient : IAsyncDisposable
{
    private readonly ICslQueryProvider _query;
    private readonly ICslAdminProvider _admin;
    private readonly string _database;

    public AdxClient(IConfiguration cfg)
    {
        var cluster = cfg["ADX:ClusterUri"] ?? "https://localhost";
        _database = cfg["ADX:Database"] ?? "ioc";
        var kcsb = new KustoConnectionStringBuilder(cluster, _database).WithAadUserPromptAuthentication();
        _query = KustoClientFactory.CreateCslQueryProvider(kcsb);
        _admin = KustoClientFactory.CreateCslAdminProvider(kcsb);
    }

    public Task<IDataReader> QueryAsync(string kql, ClientRequestProperties? props = null)
        => Task.FromResult(_query.ExecuteQuery(_database, kql, props));

    public Task<IDataReader> CommandAsync(string kql)
        => Task.FromResult(_admin.ExecuteControlCommand(_database, kql));

    public ValueTask DisposeAsync()
    {
        _query.Dispose();
        _admin.Dispose();
        return ValueTask.CompletedTask;
    }
}
