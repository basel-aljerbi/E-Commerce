using System.Net;
using System.Net.Http.Json;
using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Models;
using E_Commerce.API.Services;
using E_Commerce.API.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace E_Commerce.API.Tests.Integration;

public class AuthFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var uniqueEmail = $"register_{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync("/auth/register", new RegisterDto(
            uniqueEmail, "TestPass1!", "Test User"));

        var parsed = await _factory.ParseResponseAsync(response);
        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
        Assert.Contains("Registration successful", parsed.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var email = $"duplicate_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync("/auth/register", new RegisterDto(email, "TestPass1!", "First"));

        var response = await client.PostAsJsonAsync("/auth/register", new RegisterDto(email, "TestPass2!", "Second"));
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, parsed.StatusCode);
        Assert.False(parsed.Success);
        Assert.Contains("already registered", parsed.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsValidationError()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.PostAsJsonAsync("/auth/register", new RegisterDto(
            "weak@test.com", "short", "Weak User"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_WithValidCode_ReturnsSuccess()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var email = $"verify_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync("/auth/register", new RegisterDto(email, "TestPass1!", "Verify User"));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);
        var verificationCode = "dummy";
        var codeHash = JwtService.ComputeTokenHash(verificationCode);
        user.EmailVerificationCodeHash = codeHash;
        user.EmailVerificationCodeExpires = DateTime.UtcNow.AddMinutes(15);
        await db.SaveChangesAsync();

        var response = await client.PostAsJsonAsync("/auth/verify-email", new VerifyEmailDto(email, verificationCode));
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var email = $"login_{Guid.NewGuid():N}@test.com";
        var password = "TestPass1!";

        await client.PostAsJsonAsync("/auth/register", new RegisterDto(email, password, "Login User"));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);
            user.IsEmailVerified = true;
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/auth/login", new LoginDto(email, password));
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
        Assert.NotNull(parsed.Data);

        var data = parsed.Data!.Value;
        Assert.True(data.TryGetProperty("token", out var token));
        Assert.True(data.TryGetProperty("refreshToken", out var refreshToken));
        Assert.False(string.IsNullOrWhiteSpace(token.GetString()));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken.GetString()));
    }

    [Fact]
    public async Task Login_WithWrongPassword_IncrementsFailedAttempts()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var email = $"lockout_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync("/auth/register", new RegisterDto(email, "TestPass1!", "Lockout User"));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);
            user.IsEmailVerified = true;
            await db.SaveChangesAsync();
        }

        for (int i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/auth/login", new LoginDto(email, "WrongPass1!"));
        }

        var response = await client.PostAsJsonAsync("/auth/login", new LoginDto(email, "TestPass1!"));
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, parsed.StatusCode);
        Assert.Contains("locked", parsed.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var email = $"refresh_{Guid.NewGuid():N}@test.com";
        var password = "TestPass1!";

        await client.PostAsJsonAsync("/auth/register", new RegisterDto(email, password, "Refresh User"));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);
            user.IsEmailVerified = true;
            await db.SaveChangesAsync();
        }

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginDto(email, password));
        var loginParsed = await _factory.ParseResponseAsync(loginResponse);
        var refreshToken = loginParsed.Data!.Value.GetProperty("refreshToken").GetString()!;

        var response = await client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(refreshToken));
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);

        var newRefreshToken = parsed.Data!.Value.GetProperty("refreshToken").GetString()!;
        Assert.NotEqual(refreshToken, newRefreshToken);
    }

    [Fact]
    public async Task RefreshToken_WithRevokedToken_DetectsReuseAndRevokesFamily()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var email = $"reuse_{Guid.NewGuid():N}@test.com";
        var password = "TestPass1!";

        await client.PostAsJsonAsync("/auth/register", new RegisterDto(email, password, "Reuse User"));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);
            user.IsEmailVerified = true;
            await db.SaveChangesAsync();
        }

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginDto(email, password));
        var loginParsed = await _factory.ParseResponseAsync(loginResponse);
        var originalRefreshToken = loginParsed.Data!.Value.GetProperty("refreshToken").GetString()!;

        // First refresh: valid, rotates token
        var refresh1 = await client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(originalRefreshToken));
        Assert.Equal(HttpStatusCode.OK, refresh1.StatusCode);

        // Second refresh with SAME old token: reuse detected
        var refresh2 = await client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(originalRefreshToken));
        var parsed2 = await _factory.ParseResponseAsync(refresh2);

        Assert.Equal(HttpStatusCode.BadRequest, parsed2.StatusCode);
        Assert.Contains("reuse", parsed2.Message, StringComparison.OrdinalIgnoreCase);

        // The rotated token should also be revoked now
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);
            var userTokens = await db.RefreshTokens
                .Where(t => t.UserId == user.Id)
                .ToListAsync();
            Assert.NotEmpty(userTokens);
            Assert.All(userTokens, t => Assert.True(t.IsRevoked));
        }
    }

    [Fact]
    public async Task Logout_RevokesAllTokens()
    {
        var email = $"logout_{Guid.NewGuid():N}@test.com";
        var client = _factory.CreateAuthenticatedClient(email: email);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);
            var jti = Guid.NewGuid().ToString();
            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = JwtService.ComputeTokenHash("test-token"),
                JwtId = jti,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsync("/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);
            var userTokens = await db.RefreshTokens
                .Where(t => t.UserId == user.Id)
                .ToListAsync();
            Assert.NotEmpty(userTokens);
            Assert.All(userTokens, t => Assert.True(t.IsRevoked));
        }
    }

    [Fact]
    public async Task ForgotPassword_AlwaysReturnsSameMessage()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var email = $"forgot_{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync("/auth/forgot-password", new ForgotPasswordDto(email));
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.Contains("If the email exists", parsed.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnauthenticatedAccessToProtectedEndpoint_Returns401()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/cart");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedAccessToProtectedEndpoint_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/cart");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
