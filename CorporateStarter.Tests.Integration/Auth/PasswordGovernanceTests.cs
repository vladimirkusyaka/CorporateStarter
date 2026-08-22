using CorporateStarter.Shared.Dtos.Auth;
using CorporateStarter.Shared.Dtos.Security.Roles;
using CorporateStarter.Shared.Dtos.Security.Users;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace CorporateStarter.Tests.Integration.Auth
{
    public sealed class PasswordGovernanceTests : IClassFixture<CorporateStarterApiFactory>
    {
        private readonly HttpClient _client;

        public PasswordGovernanceTests(CorporateStarterApiFactory factory)
        {
            _client = factory.CreateClient(
                new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    HandleCookies = false
                });
        }

        [Fact]
        public async Task CreateUser_WithWeakPassword_ReturnsPasswordPolicyError()
        {
            var accessToken = await LoginAsAdminAsync();
            var roleId = await GetAnyActiveRoleIdAsync(accessToken);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/Users");

            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

            request.Content = JsonContent.Create(
                new CreateUserRequest
                {
                    Login = "weak-password-user-" + Guid.NewGuid().ToString("N")[..8],
                    Email = $"weak-{Guid.NewGuid():N}@corporatestarter.test",
                    DisplayName = "Weak Password User",
                    Password = "password",
                    IsActive = true,
                    RoleIds = [roleId]
                });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("user.password_policy_failed", await ReadErrorCodeAsync(response));
        }

        [Fact]
        public async Task ChangePassword_ReusingRecentPassword_ReturnsPasswordReusedError()
        {
            var accessToken = await LoginAsAdminAsync();
            var roleId = await GetAnyActiveRoleIdAsync(accessToken);

            var userId = await CreateUserAsync(
                accessToken,
                roleId,
                "Initial123!Strong",
                isActive: true);

            var newPassword = "Changed123!Strong";

            var firstChangeResponse = await ChangePasswordAsync(
                accessToken,
                userId,
                newPassword);

            Assert.Equal(HttpStatusCode.OK, firstChangeResponse.StatusCode);

            var reusedPasswordResponse = await ChangePasswordAsync(
                accessToken,
                userId,
                newPassword);

            Assert.Equal(HttpStatusCode.BadRequest, reusedPasswordResponse.StatusCode);
            Assert.Equal("user.password_reused", await ReadErrorCodeAsync(reusedPasswordResponse));
        }

        private async Task<string> LoginAsAdminAsync()
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

            return body.AccessToken;
        }

        private async Task<Guid> GetAnyActiveRoleIdAsync(string accessToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/Roles");

            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var roles = await response.Content.ReadFromJsonAsync<IReadOnlyList<RoleListItemDto>>();

            Assert.NotNull(roles);

            var role = Assert.Single(roles!, x => x.IsActive && x.Name == "Administrator");

            return role.Id;
        }

        private async Task<Guid> CreateUserAsync(
            string accessToken,
            Guid roleId,
            string password,
            bool isActive)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/Users");

            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

            var suffix = Guid.NewGuid().ToString("N")[..8];

            request.Content = JsonContent.Create(
                new CreateUserRequest
                {
                    Login = "password-test-" + suffix,
                    Email = $"password-test-{suffix}@corporatestarter.test",
                    DisplayName = "Password Test User",
                    Password = password,
                    IsActive = isActive,
                    RoleIds = [roleId]
                });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await ReadGuidValueAsync(response);
        }

        private async Task<HttpResponseMessage> ChangePasswordAsync(
            string accessToken,
            Guid userId,
            string newPassword)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/Users/{userId}/password");

            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

            request.Content = JsonContent.Create(
                new ChangeUserPasswordRequest
                {
                    NewPassword = newPassword
                });

            return await _client.SendAsync(request);
        }

        private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);

            return document.RootElement.TryGetProperty("errorCode", out var errorCode)
                ? errorCode.GetString()
                : null;
        }

        private static async Task<Guid> ReadGuidValueAsync(HttpResponseMessage response)
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);

            var value = document.RootElement.GetProperty("value").GetString();

            return Guid.Parse(value!);
        }
    }
}
