using System.Net;
using E_Commerce.API.Tests.Helpers;

namespace E_Commerce.API.Tests.Integration;

public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthCheckTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Endpoint_ReturnsOk()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_Endpoint_ReturnsOk()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthLive_Endpoint_ReturnsOk()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_AlwaysAllowed_WithoutAuth()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
