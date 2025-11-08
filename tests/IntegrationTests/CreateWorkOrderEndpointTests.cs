using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using IOC.Application.Work.CreateWorkOrder;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IOC.IntegrationTests;

public class CreateWorkOrderEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CreateWorkOrderEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task Returns_201_On_Success()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cmd = new CreateWorkOrderCommand("Title", "Desc", "site-1", "asset-1");
        var response = await client.PostAsJsonAsync("/api/v1/work/orders", cmd);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
