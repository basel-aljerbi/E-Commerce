using System.Net;
using System.Net.Http.Json;
using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace E_Commerce.API.Tests.Integration;

public class ReviewTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReviewTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateReview_ValidRequest_ReturnsCreated()
    {
        var client = _factory.CreateAuthenticatedClient("reviewuser@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Reviewed Product", 50m, 100);
        }

        var response = await client.PostAsJsonAsync($"/products/{productId}/reviews",
            new CreateReviewDto(5, "Excellent product!"));
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task CreateReview_WithoutAuth_Returns401()
    {
        var client = _factory.CreateUnauthenticatedClient();
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Unauth Review Product", 50m, 100);
        }

        var response = await client.PostAsJsonAsync($"/products/{productId}/reviews",
            new CreateReviewDto(4, "Nice"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateReview_Duplicate_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient("dupereview@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Duplicate Review Product", 50m, 100);
        }

        await client.PostAsJsonAsync($"/products/{productId}/reviews",
            new CreateReviewDto(4, "First review"));

        var response = await client.PostAsJsonAsync($"/products/{productId}/reviews",
            new CreateReviewDto(3, "Second review attempt"));
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, parsed.StatusCode);
        Assert.Contains("already reviewed", parsed.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetReviews_ReturnsProductReviews()
    {
        var client = _factory.CreateUnauthenticatedClient();
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Get Review Product", 50m, 100);

            var authClient = _factory.CreateAuthenticatedClient("reviewer1@test.com");
            await authClient.PostAsJsonAsync($"/products/{productId}/reviews",
                new CreateReviewDto(5, "Great!"));
        }

        var response = await client.GetAsync($"/products/{productId}/reviews");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
        Assert.NotNull(parsed.Data);

        var reviews = parsed.Data!.Value.GetProperty("reviews");
        Assert.NotEmpty(reviews.EnumerateArray());
        Assert.Equal(5, reviews[0].GetProperty("rating").GetInt32());
    }

    [Fact]
    public async Task DeleteReview_AsOwner_ReturnsNoContent()
    {
        var client = _factory.CreateAuthenticatedClient("ownerdel@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Delete Review Product", 50m, 100);
        }

        var createResponse = await client.PostAsJsonAsync($"/products/{productId}/reviews",
            new CreateReviewDto(3, "Okay"));
        var created = await _factory.ParseResponseAsync(createResponse);
        var reviewId = created.Data!.Value.GetInt32();

        var response = await client.DeleteAsync($"/products/{productId}/reviews/{reviewId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteReview_AsNonOwner_ReturnsForbidden()
    {
        var ownerClient = _factory.CreateAuthenticatedClient("owner@test.com");
        var otherClient = _factory.CreateAuthenticatedClient("other@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Forbidden Review Product", 50m, 100);
        }

        var createResponse = await ownerClient.PostAsJsonAsync($"/products/{productId}/reviews",
            new CreateReviewDto(4, "Mine"));
        var created = await _factory.ParseResponseAsync(createResponse);
        var reviewId = created.Data!.Value.GetInt32();

        var response = await otherClient.DeleteAsync($"/products/{productId}/reviews/{reviewId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteReview_AsAdmin_ReturnsNoContent()
    {
        var adminClient = _factory.CreateAdminClient();
        var userClient = _factory.CreateAuthenticatedClient("userforadmin@test.com");
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db, "Admin Review Product", 50m, 100);
        }

        var createResponse = await userClient.PostAsJsonAsync($"/products/{productId}/reviews",
            new CreateReviewDto(2, "Meh"));
        var created = await _factory.ParseResponseAsync(createResponse);
        var reviewId = created.Data!.Value.GetInt32();

        var response = await adminClient.DeleteAsync($"/products/{productId}/reviews/{reviewId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
