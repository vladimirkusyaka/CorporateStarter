using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Shared.Dtos.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;

namespace CorporateStarter.Tests.Integration.Auth;

public sealed class SessionIdleDisabledTests : IClassFixture<CorporateStarterApiFactory>, IDisposable
{
    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _app;
    private readonly HttpClient _client;

    public SessionIdleDisabledTests(CorporateStarterApiFactory factory)
    {
        // A separate API host per test gives each test its own rate limiter.
        // The class fixture still owns the shared PostgreSQL container.
        _app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.PostConfigure<SessionIdleOptions>(options => options.TimeoutMinutes = 0)));
        _client = _app.CreateClient(new()
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
    }

    public void Dispose()
    {
        _client.Dispose();
        _app.Dispose();
    }

    [Fact]
    public async Task Zero_AllowsOldActivity_Status_Jwt_Refresh_AndSessionList()
    {
        var s = await Login();
        await SetActivity(s, DateTime.UtcNow.AddDays(-2));
        Assert.Equal(HttpStatusCode.OK, (await Get(s, "me")).StatusCode);
        var list = await Get(s, "sessions");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var sessions = await list.Content.ReadFromJsonAsync<List<AuthSessionDto>>();
        Assert.Contains(sessions!, x => x.Id == s.Id);

        var status = await Post(s, "session/status");
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        using var json = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null,
            json.RootElement.GetProperty("remainingMilliseconds").ValueKind);
        Assert.True(await ReadActivity(s) < DateTime.UtcNow.AddDays(-1));

        Assert.Equal(HttpStatusCode.OK, (await Post(s, "session/activity")).StatusCode);
        await SetActivity(s, DateTime.UtcNow.AddDays(-2));
        Assert.Equal(HttpStatusCode.OK, (await Post(s, "refresh")).StatusCode);
        Assert.True(await ReadActivity(s) < DateTime.UtcNow.AddDays(-1));
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("blocked")]
    [InlineData("refresh-expired")]
    public async Task Zero_PreservesSecurityChecks(string scenario)
    {
        var s = await Login();
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            if (scenario == "revoked")
                Assert.Equal(HttpStatusCode.NoContent, (await Post(s, "logout")).StatusCode);
            else if (scenario == "blocked")
                await db.Users.Where(x => x.Id == s.UserId).ExecuteUpdateAsync(
                    u => u.SetProperty(x => x.IsActive, false));
            else
                await db.RefreshTokens.Where(x => x.AuthSessionId == s.Id).ExecuteUpdateAsync(
                    u => u.SetProperty(x => x.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));

            Assert.Equal(HttpStatusCode.Unauthorized, (await Post(s, "session/status")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Post(s, "session/activity")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Post(s, "refresh")).StatusCode);
            if (scenario != "refresh-expired")
                Assert.Equal(HttpStatusCode.Unauthorized, (await Get(s, "me")).StatusCode);
        }
        finally
        {
            if (scenario == "blocked")
                await db.Users.Where(x => x.Id == s.UserId).ExecuteUpdateAsync(
                    u => u.SetProperty(x => x.IsActive, true));
        }
    }

    private async Task<HttpResponseMessage> Get(Session s, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Auth/" + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", s.Access);
        return await _client.SendAsync(request);
    }

    private async Task<Session> Login()
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginRequest { Login = "admin", Password = "Admin123!ChangeMe" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        string Cookie(string name) => response.Headers.GetValues("Set-Cookie")
            .Single(x => x.StartsWith(name + "=", StringComparison.Ordinal))
            .Split(';')[0][(name.Length + 1)..];
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        return new Session(Guid.Parse(jwt.Claims.Single(x => x.Type == "auth_session_id").Value),
            body.User.Id, body.AccessToken, Cookie("__Host-corporate_starter_refresh"),
            Cookie("__Host-corporate_starter_csrf"));
    }

    private async Task<HttpResponseMessage> Post(Session s, string path, bool csrf = true)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/" + path);
        TestCookieHelper.AddCookies(request,
            ("__Host-corporate_starter_refresh", s.Refresh),
            ("__Host-corporate_starter_csrf", s.Csrf));
        if (csrf) request.Headers.Add("X-CSRF-TOKEN", s.Csrf);
        request.Headers.Add("X-Session-User", s.UserId.ToString());
        return await _client.SendAsync(request);
    }

    private async Task SetActivity(Session s, DateTime value)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // PostgreSQL timestamps have microsecond precision.
        value = new DateTime(value.Ticks - value.Ticks % 10, DateTimeKind.Utc);
        await db.AuthSessions.Where(x => x.Id == s.Id).ExecuteUpdateAsync(
            u => u.SetProperty(x => x.LastUserActivityAtUtc, value));
    }

    private async Task<DateTime> ReadActivity(Session s)
    {
        using var scope = _app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .AuthSessions.Where(x => x.Id == s.Id).Select(x => x.LastUserActivityAtUtc).SingleAsync();
    }

    private sealed record Session(Guid Id, Guid UserId, string Access, string Refresh, string Csrf);
}
