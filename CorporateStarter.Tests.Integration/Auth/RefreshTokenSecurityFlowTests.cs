using CorporateStarter.Shared.Dtos.Auth;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace CorporateStarter.Tests.Integration.Auth
{
    public sealed class RefreshTokenSecurityFlowTests : IClassFixture<CorporateStarterApiFactory>
    {
        private const string RefreshCookieName = "__Host-corporate_starter_refresh";
        private const string CsrfCookieName = "__Host-corporate_starter_csrf";
        private const string CsrfHeaderName = "X-CSRF-TOKEN";

        private readonly HttpClient _client;

        public RefreshTokenSecurityFlowTests(CorporateStarterApiFactory factory)
        {
            _client = factory.CreateClient(
                new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    HandleCookies = false
                });
        }

        [Fact]
        public async Task Refresh_ReusingOldRefreshToken_RevokesSessionAndRejectsNextRefresh()
        {
            var loginResponse = await LoginAsync();

            var firstRefreshToken = TestCookieHelper.GetCookieValue(
                loginResponse,
                RefreshCookieName);

            var firstCsrfToken = TestCookieHelper.GetCookieValue(
                loginResponse,
                CsrfCookieName);

            var firstRefreshResponse = await RefreshAsync(
                firstRefreshToken,
                firstCsrfToken);

            Assert.Equal(HttpStatusCode.OK, firstRefreshResponse.StatusCode);

            var secondRefreshToken = TestCookieHelper.GetCookieValue(
                firstRefreshResponse,
                RefreshCookieName);

            var secondCsrfToken = TestCookieHelper.GetCookieValue(
                firstRefreshResponse,
                CsrfCookieName);

            var reuseOldRefreshResponse = await RefreshAsync(
                firstRefreshToken,
                secondCsrfToken);

            Assert.Equal(HttpStatusCode.Unauthorized, reuseOldRefreshResponse.StatusCode);

            var refreshAfterReuseDetectionResponse = await RefreshAsync(
                secondRefreshToken,
                secondCsrfToken);

            Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterReuseDetectionResponse.StatusCode);
        }

        private async Task<HttpResponseMessage> LoginAsync()
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

            return response;
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
    }
}
