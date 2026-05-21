using System.Net;
using System.Net.Http.Json;
using E_Commerce.API.Data;
using E_Commerce.API.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace E_Commerce.API.Tests.Integration;

public class WishlistTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WishlistTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddToWishlist_ThenRemove_CompletesSuccessfully()
    {
        var client = _factory.CreateAuthenticatedClient("wishlistuser@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Wishlist Item", 15m, 50);
        }

        // Act - Add to wishlist
        var addResponse = await client.PostAsync($"/wishlist/{productId}", null);
        var addParsed = await _factory.ParseResponseAsync(addResponse);

        Assert.Equal(HttpStatusCode.Created, addParsed.StatusCode);
        Assert.True(addParsed.Success);

        // Assert - Item is in wishlist
        var getResponse = await client.GetAsync("/wishlist");
        var getParsed = await _factory.ParseResponseAsync(getResponse);

        Assert.Equal(HttpStatusCode.OK, getParsed.StatusCode);
        Assert.True(getParsed.Success);
        Assert.NotNull(getParsed.Data);
        var items = getParsed.Data!.Value.EnumerateArray().ToList();
        Assert.Contains(items, i => i.GetProperty("productId").GetInt32() == productId);

        // Act - Remove from wishlist
        var deleteResponse = await client.DeleteAsync($"/wishlist/{productId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Assert - Item is removed
        var getAfterDeleteResponse = await client.GetAsync("/wishlist");
        var getAfterDeleteParsed = await _factory.ParseResponseAsync(getAfterDeleteResponse);

        Assert.Equal(HttpStatusCode.OK, getAfterDeleteParsed.StatusCode);
        Assert.True(getAfterDeleteParsed.Success);
        var itemsAfterDelete = getAfterDeleteParsed.Data!.Value.EnumerateArray().ToList();
        Assert.DoesNotContain(itemsAfterDelete, i => i.GetProperty("productId").GetInt32() == productId);
    }
}
