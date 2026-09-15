using System.Net;
using System.Net.Http.Json;
using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CorporateStarter.Tests.Integration.Auth
{
    public sealed class RefreshConcurrencyTests
    : IClassFixture<CorporateStarterApiFactory>
    {
        private const string RefreshCookie =
            "__Host-corporate_starter_refresh";

        private const string CsrfCookie =
            "__Host-corporate_starter_csrf";

        private readonly CorporateStarterApiFactory _factory;

        public RefreshConcurrencyTests(CorporateStarterApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ConcurrentRefresh_SameToken_RetriesThenDetectsReuse()
        {
            var barrier = new RefreshReadBarrier();

            await using var app = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                    services.ConfigureDbContext<AppDbContext>(options =>
                        options.AddInterceptors(barrier))));

            using var client = app.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    HandleCookies = false
                });

            using var timeout =
                new CancellationTokenSource(TimeSpan.FromSeconds(45));

            var ct = timeout.Token;

            using var login = await client.PostAsJsonAsync(
                "/api/Auth/login",
                new LoginRequest
                {
                    Login = "admin",
                    Password = "Admin123!ChangeMe"
                },
                ct);

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

            var oldToken = TestCookieHelper.GetCookieValue(
                login, RefreshCookie);

            var csrf = TestCookieHelper.GetCookieValue(
                login, CsrfCookie);

            using var firstRequest = CreateRefresh(oldToken, csrf);
            using var secondRequest = CreateRefresh(oldToken, csrf);

            var responses = await Task.WhenAll(
                client.SendAsync(firstRequest, ct),
                client.SendAsync(secondRequest, ct));

            using var first = responses[0];
            using var second = responses[1];

            Assert.Equal(
                1,
                responses.Count(r => r.StatusCode == HttpStatusCode.OK));

            Assert.Equal(
                1,
                responses.Count(r =>
                    r.StatusCode == HttpStatusCode.Unauthorized));

            Assert.True(
                barrier.ReadCount >= 3,
                "Expected a fresh token read during retry.");

            var winner = responses.Single(
                r => r.StatusCode == HttpStatusCode.OK);

            var nextToken = TestCookieHelper.GetCookieValue(
                winner, RefreshCookie);

            var nextCsrf = TestCookieHelper.GetCookieValue(
                winner, CsrfCookie);

            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var tokens = scope.ServiceProvider
                    .GetRequiredService<IRefreshTokenService>();

                var oldHash = tokens.HashToken(oldToken);
                var nextHash = tokens.HashToken(nextToken);

                var original = await db.RefreshTokens
                    .AsNoTracking()
                    .Include(x => x.AuthSession)
                    .Include(x => x.RefreshTokenFamily)
                    .SingleAsync(x => x.TokenHash == oldHash, ct);

                Assert.NotEqual(oldHash, nextHash);
                Assert.Equal(nextHash, original.ReplacedByTokenHash);
                Assert.NotNull(original.RevokedAtUtc);
                Assert.NotNull(original.AuthSession.RevokedAtUtc);
                Assert.NotNull(original.RefreshTokenFamily.RevokedAtUtc);
                Assert.NotNull(original.RefreshTokenFamily.ReuseDetectedAtUtc);

                Assert.Equal(
                    2,
                    await db.RefreshTokens.CountAsync(
                        x => x.RefreshTokenFamilyId ==
                             original.RefreshTokenFamilyId,
                        ct));

                Assert.Equal(
                    1,
                    await db.SecurityEvents.CountAsync(
                        x => x.AuthSessionId == original.AuthSessionId &&
                             x.EventType == "RefreshSucceeded",
                        ct));

                Assert.Equal(
                    1,
                    await db.SecurityEvents.CountAsync(
                        x => x.AuthSessionId == original.AuthSessionId &&
                             x.EventType == "RefreshTokenReuseDetected",
                        ct));
            }

            using var afterReuseRequest = CreateRefresh(nextToken, nextCsrf);
            using var afterReuse = await client.SendAsync(afterReuseRequest, ct);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                afterReuse.StatusCode);
        }

        private static HttpRequestMessage CreateRefresh(
            string token,
            string csrf)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/Auth/refresh");

            TestCookieHelper.AddCookies(
                request,
                (RefreshCookie, token),
                (CsrfCookie, csrf));

            request.Headers.Add("X-CSRF-TOKEN", csrf);

            return request;
        }
    }
}
