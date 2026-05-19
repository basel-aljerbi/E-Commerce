using System.Net;
using System.Net.Http.Json;
using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace E_Commerce.API.Tests.Integration;

public class ProductTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        // Clean database before each test to prevent cross-test contamination
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            db.Products.RemoveRange(db.Products);
            db.Categories.RemoveRange(db.Categories);
            db.SaveChanges();
        }
    }

    [Fact]
    public async Task GetProducts_ReturnsPagedResult()
    {
        var client = _factory.CreateUnauthenticatedClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            _factory.SeedCategory(db, "Electronics");
            _factory.SeedProduct(db, "Product A", 10m, 50);
            _factory.SeedProduct(db, "Product B", 20m, 30);
        }

        var response = await client.GetAsync("/products");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
        Assert.NotNull(parsed.Data);

        var data = parsed.Data!.Value;
        Assert.True(data.TryGetProperty("items", out var items));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.Equal(2, totalCount.GetInt32());
    }

    [Fact]
    public async Task GetProductById_ReturnsProduct()
    {
        var client = _factory.CreateUnauthenticatedClient();
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db);
        }

        var response = await client.GetAsync($"/products/{productId}");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
        Assert.NotNull(parsed.Data);
    }

    [Fact]
    public async Task GetProductById_NotFound_Returns404()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/products/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_ReturnsCreated()
    {
        var client = _factory.CreateAdminClient();
        int categoryId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            _factory.SeedCategory(db);
            categoryId = db.Categories.First().Id;
        }

        using var formContent = new MultipartFormDataContent
        {
            { new StringContent("New Product"), "Name" },
            { new StringContent("Description"), "Description" },
            { new StringContent("49.99"), "Price" },
            { new StringContent("100"), "StockQuantity" },
            { new StringContent(categoryId.ToString()), "CategoryId" }
        };

        var response = await client.PostAsync("/products", formContent);
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task CreateProduct_AsUser_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient();
        int categoryId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            _factory.SeedCategory(db);
            categoryId = db.Categories.First().Id;
        }

        using var formContent = new MultipartFormDataContent
        {
            { new StringContent("New Product"), "Name" },
            { new StringContent("Description"), "Description" },
            { new StringContent("49.99"), "Price" },
            { new StringContent("100"), "StockQuantity" },
            { new StringContent(categoryId.ToString()), "CategoryId" }
        };

        var response = await client.PostAsync("/products", formContent);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithoutAuth_Returns401()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.PostAsync("/products", new MultipartFormDataContent
        {
            { new StringContent("Test"), "Name" },
            { new StringContent("Desc"), "Description" },
            { new StringContent("10"), "Price" },
            { new StringContent("10"), "StockQuantity" },
            { new StringContent("1"), "CategoryId" }
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_AsAdmin_ReturnsNoContent()
    {
        var client = _factory.CreateAdminClient();
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            productId = _factory.SeedProduct(db);
        }

        var response = await client.DeleteAsync($"/products/{productId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SearchProducts_ByKeyword_ReturnsFiltered()
    {
        var client = _factory.CreateUnauthenticatedClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            _factory.SeedCategory(db, "Books");
            var catId = db.Categories.First().Id;
            _factory.SeedProduct(db, "C# Programming", 39.99m, 10, catId);
            _factory.SeedProduct(db, "Python Programming", 29.99m, 10, catId);
            _factory.SeedProduct(db, "Gardening Guide", 15m, 5, catId);
        }

        var response = await client.GetAsync("/products?search=Programming");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        var items = parsed.Data!.Value.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
    }
}
