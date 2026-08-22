using CorporateStarter.Shared.Dtos.Auth;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace CorporateStarter.Tests.Integration.Auth
{
    public sealed class SessionGovernanceTests : IClassFixture<CorporateStarterApiFactory>
    {
        private const string RefreshCookieName = "__Host-corporate_starter_refresh";
        private const string CsrfCookieName = "__Host-corporate_starter_csrf";
        private const string CsrfHeaderName = "X-CSRF-TOKEN";

        private readonly HttpClient _client;

        public SessionGovernanceTests(CorporateStarterApiFactory factory)
        {
            _client = factory.CreateClient(
                new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    HandleCookies = false
                });
        }

        [Fact]
        public async Task Sessions_ReturnsCurrentActiveSession()
        {
            var login = await LoginAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/Auth/sessions");

            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

            var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized &&
                response.Headers.TryGetValues("X-Test-Auth-Failed", out var authErrors))
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, authErrors));
            }

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var sessions = await response.Content.ReadFromJsonAsync<IReadOnlyList<AuthSessionDto>>();

            Assert.NotNull(sessions);
            Assert.NotEmpty(sessions!);
            Assert.Contains(sessions!, x => x.IsCurrent);
        }

        [Fact]
        public async Task RevokeSession_RevokesSpecificSessionAndRejectsNextRefresh()
        {
            var login = await LoginAsync();

            var sessions = await GetSessionsAsync(login.AccessToken);
            var currentSession = Assert.Single(sessions, x => x.IsCurrent);

            using var revokeRequest = new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/Auth/sessions/{currentSession.Id}");

            revokeRequest.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

            var revokeResponse = await _client.SendAsync(revokeRequest);

            Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

            var refreshResponse = await RefreshAsync(
                login.RefreshToken,
                login.CsrfToken);

            Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        }

        [Fact]
        public async Task LogoutAll_RevokesAllUserSessions()
        {
            var firstLogin = await LoginAsync();
            var secondLogin = await LoginAsync();

            using var logoutAllRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/Auth/logout-all");

            logoutAllRequest.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                firstLogin.AccessToken);

            TestCookieHelper.AddCookies(
                logoutAllRequest,
                (RefreshCookieName, firstLogin.RefreshToken),
                (CsrfCookieName, firstLogin.CsrfToken));

            logoutAllRequest.Headers.Add(CsrfHeaderName, firstLogin.CsrfToken);

            var logoutAllResponse = await _client.SendAsync(logoutAllRequest);

            Assert.Equal(HttpStatusCode.NoContent, logoutAllResponse.StatusCode);

            var firstRefreshAfterLogoutAll = await RefreshAsync(
                firstLogin.RefreshToken,
                firstLogin.CsrfToken);

            var secondRefreshAfterLogoutAll = await RefreshAsync(
                secondLogin.RefreshToken,
                secondLogin.CsrfToken);

            Assert.Equal(HttpStatusCode.Unauthorized, firstRefreshAfterLogoutAll.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, secondRefreshAfterLogoutAll.StatusCode);
        }

        private async Task<LoginTestSession> LoginAsync()
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

            var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
    .ReadJwtToken(body!.AccessToken);

            Console.WriteLine($"JWT issuer: {jwt.Issuer}");
            Console.WriteLine($"JWT audiences: {string.Join(", ", jwt.Audiences)}");

            Console.WriteLine($"JWT kid: {jwt.Header.Kid}");
            Console.WriteLine($"JWT issuer: {jwt.Issuer}");
            Console.WriteLine($"JWT audiences: {string.Join(", ", jwt.Audiences)}");

            Assert.NotNull(body);
            Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));

            var refreshToken = TestCookieHelper.GetCookieValue(
                response,
                RefreshCookieName);

            var csrfToken = TestCookieHelper.GetCookieValue(
                response,
                CsrfCookieName);

            return new LoginTestSession(
                body.AccessToken,
                refreshToken,
                csrfToken);
        }

        private async Task<IReadOnlyList<AuthSessionDto>> GetSessionsAsync(
            string accessToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/Auth/sessions");

            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

            var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized &&
                response.Headers.TryGetValues("X-Test-Auth-Failed", out var authErrors))
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, authErrors));
            }

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var sessions = await response.Content.ReadFromJsonAsync<IReadOnlyList<AuthSessionDto>>();

            Assert.NotNull(sessions);

            return sessions!;
        }

        private async Task<HttpResponseMessage> RefreshAsync(
            string refreshToken,
            string csrfToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/Auth/refresh");

            TestCookieHelper.AddCookies(
                request,
                (RefreshCookieName, refreshToken),
                (CsrfCookieName, csrfToken));

            request.Headers.Add(CsrfHeaderName, csrfToken);

            return await _client.SendAsync(request);
        }

        private sealed record LoginTestSession(
            string AccessToken,
            string RefreshToken,
            string CsrfToken);
    }
}
