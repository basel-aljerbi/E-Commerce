using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using E_Commerce.API.Data;
using E_Commerce.API.Models;
using E_Commerce.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace E_Commerce.API.Tests.Helpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static int _instanceCounter;
    private readonly string _dbName;

    static CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
    }

    public CustomWebApplicationFactory()
    {
        _dbName = $"TestDb_{++_instanceCounter}_{Guid.NewGuid():N}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseSetting("TestDbName", _dbName);

        builder.UseSetting("ConnectionStrings:ECommerce", "Server=.;Database=TestDb;Trusted_Connection=true;TrustServerCertificate=true;");
        builder.UseSetting("Jwt:Key", "IntegrationTestKeyThatIsAtLeast32CharactersLong!");
        builder.UseSetting("Jwt:Issuer", "TestIssuer");
        builder.UseSetting("Jwt:Audience", "TestAudience");
        builder.UseSetting("Jwt:AccessTokenExpirationMinutes", "60");
        builder.UseSetting("Stripe:SecretKey", "SK.placeholder");
        builder.UseSetting("Stripe:PublishableKey", "PK.placeholder");
        builder.UseSetting("Stripe:WebhookSecret", "WH.placeholder");
        builder.UseSetting("SendGrid:ApiKey", "sendgrid-placeholder");
        builder.UseSetting("SendGrid:FromEmail", "test@example.com");
        builder.UseSetting("SendGrid:FromName", "Test");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:3000");
        builder.UseSetting("FrontendUrl", "http://localhost:3000");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "100");
        builder.UseSetting("RateLimiting:AuthWindowInMinutes", "1");
    }

    public HttpClient CreateUnauthenticatedClient()
    {
        return CreateClient();
    }

    public HttpClient CreateAuthenticatedClient(string? email = null, string role = "User")
    {
        var client = CreateClient();
        var token = GenerateJwtToken(email, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient CreateAdminClient()
    {
        return CreateAuthenticatedClient(role: "Admin");
    }

    // Creates a scoped ECommerceContext for setup/teardown within test methods.
    // This ensures the service provider is not accessed during class construction.
    public ECommerceContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ECommerceContext>();
    }

    public void SeedCategory(ECommerceContext db, string name = "Test Category")
    {
        if (!db.Categories.Any(c => c.Name == name))
        {
            db.Categories.Add(new Category { Name = name });
            db.SaveChanges();
        }
    }

    public int SeedProduct(ECommerceContext db, string name = "Test Product", decimal price = 10.99m, int stock = 100, int? categoryId = null)
    {
        if (categoryId is null)
        {
            SeedCategory(db);
            categoryId = db.Categories.First().Id;
        }

        var product = new Product
        {
            Name = name,
            Description = $"Description for {name}",
            Price = price,
            StockQuantity = stock,
            CategoryId = categoryId.Value
        };
        db.Products.Add(product);
        db.SaveChanges();
        return product.Id;
    }

    private string GenerateJwtToken(string? email, string role)
    {
        email ??= $"authtest_{Guid.NewGuid():N}@test.com";

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();

        var user = db.Users.FirstOrDefault(u => u.Email == email);
        if (user == null)
        {
            user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!", workFactor: 6),
                FullName = "Test User",
                Role = role,
                IsEmailVerified = true
            };
            db.Users.Add(user);
            db.SaveChanges();
        }

        var jwtService = scope.ServiceProvider.GetRequiredService<JwtService>();
        var (token, _, _) = jwtService.GenerateAccessToken(user);
        return token;
    }

    public async Task<ApiTestResponse> ParseResponseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return new ApiTestResponse(response.StatusCode, content);
    }
}

public class ApiTestResponse
{
    public HttpStatusCode StatusCode { get; }
    public string Raw { get; }
    public JsonDocument? Document { get; }

    public ApiTestResponse(HttpStatusCode statusCode, string raw)
    {
        StatusCode = statusCode;
        Raw = raw;
        try { Document = JsonDocument.Parse(raw); } catch { Document = null; }
    }

    public bool? Success => Document?.RootElement.GetProperty("success").GetBoolean();
    public string? Message => Document?.RootElement.TryGetProperty("message", out var m) == true ? m.GetString() : null;
    public JsonElement? Data => Document?.RootElement.TryGetProperty("data", out var d) == true ? d : null;
}
