using CorporateStarter.Shared.Dtos.Auth;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace CorporateStarter.Tests.Integration.Auth
{
    public sealed class LoginLockoutTests : IClassFixture<CorporateStarterApiFactory>
    {
        private readonly HttpClient _client;

        public LoginLockoutTests(CorporateStarterApiFactory factory)
        {
            _client = factory.CreateClient(
                new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    HandleCookies = false
                });
        }

        [Fact]
        public async Task Login_AfterFailedAttemptThreshold_BlocksEvenValidPassword()
        {
            for (var i = 0; i < 3; i++)
            {
                var failedResponse = await LoginAsync("admin", "WrongPassword123!");

                Assert.Equal(HttpStatusCode.Unauthorized, failedResponse.StatusCode);
            }

            var validPasswordResponse = await LoginAsync("admin", "Admin123!ChangeMe");

            Assert.Equal(HttpStatusCode.Unauthorized, validPasswordResponse.StatusCode);
        }

        private Task<HttpResponseMessage> LoginAsync(
            string login,
            string password)
        {
            return _client.PostAsJsonAsync(
                "/api/Auth/login",
                new LoginRequest
                {
                    Login = login,
                    Password = password
                });
        }
    }
}
