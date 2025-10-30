using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IOC.IntegrationTests;

public class LeakDetectionEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LeakDetectionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task Calibrate_And_Detect_Returns_Incident()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Calibrate with near-zero mean
        var segId = "seg-1";
        var balances = Enumerable.Repeat(0.0, 50).ToList();
        var calResp = await client.PostAsJsonAsync($"/api/v1/pipeline/{segId}/calibrate", balances);
        calResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Detect with strong imbalance
        var body = new
        {
            UpMeterId = "u1",
            Upstream_m3_s = 10.0,
            UpUncertaintyPct = 0.5,
            DnMeterId = "d1",
            Downstream_m3_s = 6.0,
            DnUncertaintyPct = 0.5,
            ElevationDelta_m = 0.0,
            Temperature_C = 20.0
        };
        var detResp = await client.PostAsJsonAsync($"/api/v1/pipeline/{segId}/detect", body);
        detResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
