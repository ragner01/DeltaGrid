using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using IOC.Application.Lab;
using IOC.Core.Domain.Lab;

using Microsoft.Extensions.Logging;

namespace IOC.Infrastructure.Persistence;

public sealed class InMemoryLabRepository : ILabRepository
{
    private static readonly ConcurrentDictionary<string, Sample> Samples = new();
    private static readonly ConcurrentDictionary<string, LabResult> ActiveResults = new();

    public Task SaveSampleAsync(Sample sample, CancellationToken ct)
    {
        Samples[sample.SampleId] = sample; return Task.CompletedTask;
    }

    public Task<Sample?> GetSampleAsync(string sampleId, CancellationToken ct)
    {
        Samples.TryGetValue(sampleId, out var s); return Task.FromResult(s);
    }

    public Task SaveResultAsync(LabResult result, CancellationToken ct)
    {
        ActiveResults[result.SampleId] = result; return Task.CompletedTask;
    }

    public Task<LabResult?> GetActiveResultAsync(string sampleId, CancellationToken ct)
    {
        ActiveResults.TryGetValue(sampleId, out var r); return Task.FromResult(r);
    }
}

public sealed class HmacPdfSigner : IPdfSigner
{
    private readonly byte[] _key;
    public HmacPdfSigner() { _key = Encoding.UTF8.GetBytes("demo-lab-cert-hmac-key"); }
    public (string Algo, string Signature) Sign(string certificateUrl)
    {
        using var hmac = new HMACSHA256(_key);
        var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(certificateUrl));
        return ("HMACSHA256", BitConverter.ToString(sig).Replace("-", "").ToLowerInvariant());
    }
}

public sealed class DurableLabPropertySink : ILabPropertySink
{
    private static readonly ConcurrentDictionary<string, (double? api, double? gor, double? wc, double? viscosity, DateTimeOffset ts)> Store = new();
    private readonly ILogger<DurableLabPropertySink> _logger;
    public DurableLabPropertySink(ILogger<DurableLabPropertySink> logger) { _logger = logger; }

    public Task PushToAllocationAsync(string sourceId, double? api, double? gor, double? wc, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        Store[sourceId] = (api, gor, wc, Store.TryGetValue(sourceId, out var prev) ? prev.viscosity : null, now);
        _logger.LogInformation("[Lab->Allocation] source={source} api={api} gor={gor} wc={wc}", sourceId, api, gor, wc);
        return Task.CompletedTask;
    }

    public Task PushToOptimizationAsync(string sourceId, double? api, double? gor, double? viscosity, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        Store[sourceId] = (Store.TryGetValue(sourceId, out var prev) ? prev.api : api, gor, Store.TryGetValue(sourceId, out var prev2) ? prev2.wc : null, viscosity, now);
        _logger.LogInformation("[Lab->Optimization] source={source} api={api} gor={gor} viscosity={visc}", sourceId, api, gor, viscosity);
        return Task.CompletedTask;
    }

    public static bool TryGet(string sourceId, out (double? api, double? gor, double? wc, double? viscosity, DateTimeOffset ts) value) => Store.TryGetValue(sourceId, out value);
}
