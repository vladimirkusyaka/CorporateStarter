using CorporateStarter.Shared.Dtos.Auth;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CorporateStarter.Tests.Integration.Auth;

public sealed class AuthSmokeTests : IClassFixture<CorporateStarterApiFactory>
{
    private readonly HttpClient _client;

    public AuthSmokeTests(CorporateStarterApiFactory factory)
    {
        _client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });
    }

    [Fact]
    public async Task Login_WithValidAdminCredentials_ReturnsAccessTokenAndRefreshCookie()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            new LoginRequest
            {
                Login = "admin",
                Password = "Admin123!ChangeMe"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.NotEqual(default, body.ExpiresAtUtc);
        Assert.Equal("admin", body.User.Login);

        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            x => x.Contains("__Host-corporate_starter_refresh", StringComparison.Ordinal));
    }
}
