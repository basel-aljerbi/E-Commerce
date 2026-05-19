using System.Net;
using E_Commerce.API.Tests.Helpers;

namespace E_Commerce.API.Tests.Integration;

public class AnalyticsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AnalyticsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnalyticsOverview_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/admin/analytics/overview");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AnalyticsOverview_NonAdmin_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/admin/analytics/overview");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnalyticsOverview_Admin_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/overview");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
        Assert.NotNull(parsed.Data);
    }

    [Fact]
    public async Task RevenueTrends_Admin_Monthly_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/revenue-trends?period=month");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task RevenueTrends_Admin_Daily_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/revenue-trends?period=day");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task LowStock_Admin_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/low-stock?threshold=5");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task UserGrowth_Admin_Monthly_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/user-growth?period=month");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task UserGrowth_Admin_Daily_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/user-growth?period=day");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task PaymentStats_Admin_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/payment-stats");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task OrderStatusBreakdown_Admin_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/order-status-breakdown");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task ReviewStats_Admin_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/review-stats");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task OrderTrends_Admin_Monthly_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/order-trends?period=month");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task TopProducts_Admin_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/top-products?top=5");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task FullAnalytics_Admin_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/full?period=month");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task FullAnalytics_Admin_DailyPeriod_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/full?period=day");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task OrderTrends_Admin_Daily_ReturnsSuccess()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/admin/analytics/order-trends?period=day");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }
}
