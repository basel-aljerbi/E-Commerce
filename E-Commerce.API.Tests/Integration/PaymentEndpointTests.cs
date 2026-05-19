using System.Net;
using E_Commerce.API.Dtos;
using E_Commerce.API.Tests.Helpers;

namespace E_Commerce.API.Tests.Integration;

public class PaymentEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PaymentEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatePaymentIntent_WithoutAuth_Returns401()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.PostAsJsonAsync("/payments/create-intent",
            new PaymentRequestDto { OrderId = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PaymentsWebhook_WithoutSignature_ReturnsBadRequest()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/payments/webhook", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PaymentsWebhook_IsAnonymous()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.PostAsync("/payments/webhook",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
