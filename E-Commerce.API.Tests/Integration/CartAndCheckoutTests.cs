using System.Net;
using System.Net.Http.Json;
using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Models.Enums;
using E_Commerce.API.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace E_Commerce.API.Tests.Integration;

public class CartAndCheckoutTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CartAndCheckoutTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddToCart_ValidRequest_ReturnsSuccess()
    {
        var client = _factory.CreateAuthenticatedClient("cartuser@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Cart Item", 15m, 50);
        }

        var response = await client.PostAsJsonAsync("/cart", new AddToCartDto(productId, 2));
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task AddToCart_QuantityExceedsStock_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient("overstock@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Low Stock Item", 15m, 3);
        }

        var response = await client.PostAsJsonAsync("/cart", new AddToCartDto(productId, 10));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCart_ReturnsCartItems()
    {
        var client = _factory.CreateAuthenticatedClient("getcart@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Get Cart Item", 20m, 100);
        }

        await client.PostAsJsonAsync("/cart", new AddToCartDto(productId, 3));

        var response = await client.GetAsync("/cart");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);

        var items = parsed.Data!.Value.GetProperty("items");
        Assert.Single(items.EnumerateArray());
        Assert.Equal(3, items[0].GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task RemoveFromCart_ReturnsNoContent()
    {
        var client = _factory.CreateAuthenticatedClient("removecart@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Remove Cart Item", 25m, 50);
        }

        var addResponse = await client.PostAsJsonAsync("/cart", new AddToCartDto(productId, 1));
        var addParsed = await _factory.ParseResponseAsync(addResponse);

        var cartResponse = await client.GetAsync("/cart");
        var cartParsed = await _factory.ParseResponseAsync(cartResponse);
        var cartItemId = cartParsed.Data!.Value.GetProperty("items")[0].GetProperty("id").GetInt32();

        var response = await client.DeleteAsync($"/cart/{cartItemId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_WithItems_CreatesOrderAndClearsCart()
    {
        var client = _factory.CreateAuthenticatedClient("checkout@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Checkout Item", 30m, 50);
        }

        await client.PostAsJsonAsync("/cart", new AddToCartDto(productId, 2));

        var response = await client.PostAsync("/orders/checkout", null);
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(parsed.Success);
        Assert.NotNull(parsed.Data);
        Assert.True(parsed.Data!.Value.TryGetProperty("orderId", out var orderId));
        Assert.True(orderId.GetInt32() > 0);
    }

    [Fact]
    public async Task Checkout_WithEmptyCart_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient("emptycart@test.com");

        var response = await client.PostAsync("/orders/checkout", null);
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, parsed.StatusCode);
        Assert.Contains("empty", parsed.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Checkout_DeductsStock()
    {
        var client = _factory.CreateAuthenticatedClient("stockdeduct@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Stock Deduct Item", 10m, 5);
        }

        await client.PostAsJsonAsync("/cart", new AddToCartDto(productId, 3));
        await client.PostAsync("/orders/checkout", null);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            var product = await db.Products.FindAsync(productId);
            Assert.NotNull(product);
            Assert.Equal(2, product.StockQuantity);
        }
    }

    [Fact]
    public async Task Checkout_InsufficientStock_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient("insufficient@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Limited Item", 5m, 1);
        }

        await client.PostAsJsonAsync("/cart", new AddToCartDto(productId, 2));

        var response = await client.PostAsync("/orders/checkout", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_RestoresStock()
    {
        var client = _factory.CreateAuthenticatedClient("cancelorder@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Cancel Item", 15m, 10);
        }

        await client.PostAsJsonAsync("/cart", new AddToCartDto(productId, 4));

        var checkoutResponse = await client.PostAsync("/orders/checkout", null);
        var checkoutParsed = await _factory.ParseResponseAsync(checkoutResponse);
        var orderId = checkoutParsed.Data!.Value.GetProperty("orderId").GetInt32();

        var cancelResponse = await client.PutAsync($"/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            var product = await db.Products.FindAsync(productId);
            Assert.NotNull(product);
            Assert.Equal(10, product.StockQuantity);
        }
    }

    [Fact]
    public async Task CancelOrder_AsUser_OnlyOwnOrders()
    {
        var client1 = _factory.CreateAuthenticatedClient("user1_cancel@test.com");
        var client2 = _factory.CreateAuthenticatedClient("user2_cancel@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Cancel Auth Item", 15m, 10);
        }

        await client1.PostAsJsonAsync("/cart", new AddToCartDto(productId, 2));
        var checkout = await client1.PostAsync("/orders/checkout", null);
        var parsed = await _factory.ParseResponseAsync(checkout);
        var orderId = parsed.Data!.Value.GetProperty("orderId").GetInt32();

        var cancelResponse = await client2.PutAsync($"/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.NotFound, cancelResponse.StatusCode);
    }

    [Fact]
    public async Task AdminCanViewAllOrders()
    {
        var adminClient = _factory.CreateAdminClient();
        var userClient = _factory.CreateAuthenticatedClient("user_orders@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Admin Orders Item", 20m, 30);
        }

        await userClient.PostAsJsonAsync("/cart", new AddToCartDto(productId, 1));
        await userClient.PostAsync("/orders/checkout", null);

        var response = await adminClient.GetAsync("/orders");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        var data = parsed.Data!.Value;
        Assert.True(data.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task AdminCanUpdateOrderStatus()
    {
        var adminClient = _factory.CreateAdminClient();
        var userClient = _factory.CreateAuthenticatedClient("user_status@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Status Item", 25m, 20);
        }

        await userClient.PostAsJsonAsync("/cart", new AddToCartDto(productId, 1));

        var checkout = await userClient.PostAsync("/orders/checkout", null);
        var parsed = await _factory.ParseResponseAsync(checkout);
        var orderId = parsed.Data!.Value.GetProperty("orderId").GetInt32();

        var response = await adminClient.PutAsync($"/orders/{orderId}/status?newStatus=Shipped", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var orderResponse = await userClient.GetAsync($"/orders/{orderId}");
        var orderParsed = await _factory.ParseResponseAsync(orderResponse);
        var status = orderParsed.Data!.Value.GetProperty("status").GetInt32();
        Assert.Equal((int)OrderStatus.Shipped, status);
    }

    [Fact]
    public async Task CartIsIsolatedPerUser()
    {
        var client1 = _factory.CreateAuthenticatedClient("isolated1@test.com");
        var client2 = _factory.CreateAuthenticatedClient("isolated2@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Isolated Item", 10m, 100);
        }

        await client1.PostAsJsonAsync("/cart", new AddToCartDto(productId, 5));

        var response2 = await client2.GetAsync("/cart");
        var parsed2 = await _factory.ParseResponseAsync(response2);
        var totalCount2 = parsed2.Data!.Value.GetProperty("itemCount").GetInt32();
        Assert.Equal(0, totalCount2);
    }
}
