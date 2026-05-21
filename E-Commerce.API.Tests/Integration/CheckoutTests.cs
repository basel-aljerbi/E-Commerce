using System.Net;
using System.Net.Http.Json;
using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace E_Commerce.API.Tests.Integration;

public class CheckoutTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CheckoutTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullCheckoutFlow_CheckoutCreatesOrder_ThenCreateIntentFailsWithStripePlaceholder()
    {
        var client = _factory.CreateAuthenticatedClient("checkoutflow@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Flow Item", 25m, 100);
        }

        // Step 1: Add to cart
        var addCartResponse = await client.PostAsJsonAsync("/cart", new AddToCartDto(productId, 2));
        var addCartParsed = await _factory.ParseResponseAsync(addCartResponse);
        Assert.Equal(HttpStatusCode.OK, addCartParsed.StatusCode);

        // Step 2: Checkout
        var checkoutResponse = await client.PostAsync("/orders/checkout", null);
        var checkoutParsed = await _factory.ParseResponseAsync(checkoutResponse);

        Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);
        Assert.True(checkoutParsed.Success);
        Assert.NotNull(checkoutParsed.Data);
        Assert.True(checkoutParsed.Data!.Value.TryGetProperty("orderId", out var orderIdElement));
        var orderId = orderIdElement.GetInt32();
        Assert.True(orderId > 0);

        // Step 3: Create payment intent (expected to fail with placeholder Stripe key)
        var paymentResponse = await client.PostAsJsonAsync("/payments/create-intent",
            new PaymentRequestDto { OrderId = orderId });

        Assert.Equal(HttpStatusCode.InternalServerError, paymentResponse.StatusCode);

        var paymentBody = await paymentResponse.Content.ReadAsStringAsync();
        Assert.Contains("Server Error", paymentBody);
    }
}
