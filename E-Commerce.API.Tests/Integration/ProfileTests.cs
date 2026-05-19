using System.Net;
using System.Net.Http.Json;
using E_Commerce.API.Dtos;
using E_Commerce.API.Tests.Helpers;

namespace E_Commerce.API.Tests.Integration;

public class ProfileTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProfileTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProfile_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_Authenticated_ReturnsProfile()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/profile");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
        Assert.NotNull(parsed.Data);
    }

    [Fact]
    public async Task GetProfile_Admin_ReturnsProfile()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/profile");
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
        Assert.NotNull(parsed.Data);
    }

    [Fact]
    public async Task UpdateProfile_ValidData_ReturnsSuccess()
    {
        var client = _factory.CreateAuthenticatedClient();
        var dto = new UpdateProfileDto(
            "Updated Name",
            "+1234567890",
            "123 Main St",
            null,
            "New York",
            "NY",
            "10001",
            "USA"
        );

        var response = await client.PutAsJsonAsync("/profile", dto);
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
    }

    [Fact]
    public async Task UpdateProfile_EmptyName_ReturnsValidationError()
    {
        var client = _factory.CreateAuthenticatedClient();
        var dto = new UpdateProfileDto(
            "",
            null, null, null, null, null, null, null
        );

        var response = await client.PutAsJsonAsync("/profile", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var dto = new UpdateProfileDto("Name", null, null, null, null, null, null, null);

        var response = await client.PutAsJsonAsync("/profile", dto);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ValidData_ReturnsSuccess()
    {
        var client = _factory.CreateAuthenticatedClient();
        var dto = new ChangePasswordDto("Password1!", "NewPass1!");

        var response = await client.PutAsJsonAsync("/profile/password", dto);
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, parsed.StatusCode);
        Assert.True(parsed.Success);
        Assert.Contains("Password changed", parsed.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();
        var dto = new ChangePasswordDto("WrongPassword1!", "NewPass1!");

        var response = await client.PutAsJsonAsync("/profile/password", dto);
        var parsed = await _factory.ParseResponseAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, parsed.StatusCode);
        Assert.False(parsed.Success);
        Assert.Contains("Current password is incorrect", parsed.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangePassword_WeakNewPassword_ReturnsValidationError()
    {
        var client = _factory.CreateAuthenticatedClient();
        var dto = new ChangePasswordDto("Password1!", "short");

        var response = await client.PutAsJsonAsync("/profile/password", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var dto = new ChangePasswordDto("Password1!", "NewPass1!");

        var response = await client.PutAsJsonAsync("/profile/password", dto);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
