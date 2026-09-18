using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Shared.Dtos.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CorporateStarter.Tests.Integration.Auth;

public sealed class SessionIdleTests : IClassFixture<CorporateStarterApiFactory>, IDisposable
{
    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _app;
    private readonly HttpClient _client;

    public SessionIdleTests(CorporateStarterApiFactory factory)
    {
        // A separate API host per test gives each test its own rate limiter.
        // The class fixture still owns the shared PostgreSQL container.
        _app = factory.WithWebHostBuilder(_ => { });
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
    public async Task ExpiredSession_RejectsUnexpiredJwt_Refresh_AndActivity()
    {
        var s = await Login();
        var old = DateTime.UtcNow.AddMinutes(-31);
        await SetActivity(s, old);
        using var me = new HttpRequestMessage(HttpMethod.Get, "/api/Auth/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", s.Access);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(me)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(s, "session/activity")).StatusCode);
        Assert.True(Math.Abs((old - await ReadActivity(s)).Ticks) < 10);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(s, "refresh")).StatusCode);
    }

    [Fact]
    public async Task StatusAndRefresh_DoNotCountAsActivity()
    {
        var s = await Login();
        var old = DateTime.UtcNow.AddMinutes(-20);
        await SetActivity(s, old);
        Assert.Equal(HttpStatusCode.OK, (await Post(s, "session/status")).StatusCode);
        Assert.True(Math.Abs((old - await ReadActivity(s)).Ticks) < 10);
        Assert.Equal(HttpStatusCode.OK, (await Post(s, "refresh")).StatusCode);
        Assert.True(Math.Abs((old - await ReadActivity(s)).Ticks) < 10);
    }

    [Fact]
    public async Task Activity_ExtendsActiveSession_WithoutAbsoluteLifetime()
    {
        var s = await Login();
        await SetActivity(s, DateTime.UtcNow.AddMinutes(-29));
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.AuthSessions.Where(x => x.Id == s.Id).ExecuteUpdateAsync(
                u => u.SetProperty(x => x.CreatedAtUtc, DateTime.UtcNow.AddDays(-2)));
        }
        var before = DateTime.UtcNow.AddSeconds(-1);
        Assert.Equal(HttpStatusCode.OK, (await Post(s, "session/activity")).StatusCode);
        Assert.True(await ReadActivity(s) >= before);
    }

    [Fact]
    public async Task Activity_RequiresCsrf_AndMatchingIdentity()
    {
        var s = await Login();
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(s, "session/activity", false)).StatusCode);
        var other = s with { UserId = Guid.NewGuid() };
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(other, "session/activity")).StatusCode);
    }

    [Fact]
    public async Task RevokedSession_CannotBeExtended()
    {
        var s = await Login();
        Assert.Equal(HttpStatusCode.NoContent, (await Post(s, "logout")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(s, "session/activity")).StatusCode);
        using var me = new HttpRequestMessage(HttpMethod.Get, "/api/Auth/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", s.Access);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(me)).StatusCode);
    }

    [Fact]
    public async Task ConcurrentActivity_NeverMovesTimestampBackwards()
    {
        var s = await Login();
        await SetActivity(s, DateTime.UtcNow.AddMinutes(-29));
        var responses = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => Post(s, "session/activity")));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.True(await ReadActivity(s) > DateTime.UtcNow.AddMinutes(-1));
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

    private Task<HttpResponseMessage> Post(Session s, string path, bool csrf = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/" + path);
        TestCookieHelper.AddCookies(request,
            ("__Host-corporate_starter_refresh", s.Refresh),
            ("__Host-corporate_starter_csrf", s.Csrf));
        if (csrf) request.Headers.Add("X-CSRF-TOKEN", s.Csrf);
        request.Headers.Add("X-Session-User", s.UserId.ToString());
        return _client.SendAsync(request);
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

public sealed class SessionIdleBoundaryTests
{
    [Fact]
    public void Boundary_IsExclusive_AtThirtyMinutes()
    {
        var policy = new SessionIdlePolicy(new SessionIdleOptions());
        var now = DateTime.UtcNow;
        Assert.True(policy.IsExpired(now.AddMinutes(-30), now));
        Assert.False(policy.IsExpired(now.AddMinutes(-30).AddTicks(1), now));
    }

}
