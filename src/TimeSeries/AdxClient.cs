using System.Data;
using System.Security;
using Microsoft.Extensions.Configuration;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;

namespace IOC.TimeSeries;

public sealed class AdxClient : IAsyncDisposable
{
    private readonly ICslQueryProvider _query;
    private readonly ICslAdminProvider _admin;
    private readonly string _database;
    private static readonly HashSet<string> AllowedKqlOperations = new()
    {
        "where", "project", "extend", "summarize", "take", "limit",
        "order", "sort", "top", "count", "distinct", "let", "as"
    };

    private static readonly HashSet<string> BlockedKqlOperations = new()
    {
        "union", "join", "database(", ".execute", ".create", ".alter", ".drop",
        ".set", ".append", ".replace", ".delete", ".move", ".rename", ".merge"
    };

    public AdxClient(IConfiguration cfg)
    {
        var cluster = cfg["ADX:ClusterUri"] ?? "https://localhost";
        _database = cfg["ADX:Database"] ?? "ioc";
        var kcsb = new KustoConnectionStringBuilder(cluster, _database).WithAadUserPromptAuthentication();
        _query = KustoClientFactory.CreateCslQueryProvider(kcsb);
        _admin = KustoClientFactory.CreateCslAdminProvider(kcsb);
    }

    public Task<IDataReader> QueryAsync(string kql, ClientRequestProperties? props = null)
    {
        if (string.IsNullOrWhiteSpace(kql))
            throw new ArgumentException("KQL query cannot be empty", nameof(kql));
        
        if (!IsKqlSafe(kql))
            throw new SecurityException($"Unsafe KQL query detected. Query contains blocked operations or suspicious patterns.");
        
        return Task.FromResult(_query.ExecuteQuery(_database, kql, props));
    }

    public Task<IDataReader> CommandAsync(string kql)
    {
        if (string.IsNullOrWhiteSpace(kql))
            throw new ArgumentException("KQL command cannot be empty", nameof(kql));
        
        if (!IsKqlSafe(kql))
            throw new SecurityException($"Unsafe KQL command detected. Command contains blocked operations or suspicious patterns.");
        
        return Task.FromResult(_admin.ExecuteControlCommand(_database, kql));
    }

    private static bool IsKqlSafe(string kql)
    {
        var lowerKql = kql.ToLowerInvariant();
        
        // Block dangerous operations
        if (BlockedKqlOperations.Any(op => lowerKql.Contains(op)))
            return false;
        
        // Validate query structure (basic check)
        if (lowerKql.Contains("..") || lowerKql.Contains("//"))
            return false;
        
        // Block database() function calls
        if (lowerKql.Contains("database("))
            return false;
        
        return true;
    }

    public ValueTask DisposeAsync()
    {
        _query.Dispose();
        _admin.Dispose();
        return ValueTask.CompletedTask;
    }
}
