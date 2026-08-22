using CorporateStarter.Shared.Dtos.Auth;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace CorporateStarter.Tests.Integration.Auth
{
    public sealed class CsrfSecurityTests : IClassFixture<CorporateStarterApiFactory>
    {
        private const string RefreshCookieName = "__Host-corporate_starter_refresh";
        private const string CsrfCookieName = "__Host-corporate_starter_csrf";
        private const string CsrfHeaderName = "X-CSRF-TOKEN";

        private readonly HttpClient _client;

        public CsrfSecurityTests(CorporateStarterApiFactory factory)
        {
            _client = factory.CreateClient(
                new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    HandleCookies = false
                });
        }

        [Fact]
        public async Task Refresh_WithoutCsrfToken_ReturnsForbidden()
        {
            var loginResponse = await LoginAsync();

            var refreshToken = TestCookieHelper.GetCookieValue(
                loginResponse,
                RefreshCookieName);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/Auth/refresh");

            TestCookieHelper.AddCookie(
                request,
                RefreshCookieName,
                refreshToken);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_WithInvalidCsrfToken_ReturnsForbidden()
        {
            var loginResponse = await LoginAsync();

            var refreshToken = TestCookieHelper.GetCookieValue(
                loginResponse,
                RefreshCookieName);

            var csrfToken = TestCookieHelper.GetCookieValue(
                loginResponse,
                CsrfCookieName);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/Auth/refresh");

            TestCookieHelper.AddCookies(
                request,
                (RefreshCookieName, refreshToken),
                (CsrfCookieName, csrfToken));

            request.Headers.Add(CsrfHeaderName, "invalid-csrf-token");

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_WithValidCsrfToken_ReturnsOk()
        {
            var loginResponse = await LoginAsync();

            var refreshToken = TestCookieHelper.GetCookieValue(
                loginResponse,
                RefreshCookieName);

            var csrfToken = TestCookieHelper.GetCookieValue(
                loginResponse,
                CsrfCookieName);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/Auth/refresh");

            TestCookieHelper.AddCookies(
                request,
                (RefreshCookieName, refreshToken),
                (CsrfCookieName, csrfToken));

            request.Headers.Add(CsrfHeaderName, csrfToken);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
    }
}
