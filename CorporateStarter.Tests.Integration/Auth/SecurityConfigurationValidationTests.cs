using CorporateStarter.Application.Common.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Xunit;

namespace CorporateStarter.Tests.Integration.Auth
{
    public sealed class SecurityConfigurationValidationTests
    {
        [Fact]
        public async Task Startup_WithMissingActiveJwtSigningKeyId_FailsFast()
        {
            await AssertStartupFailsAsync(
                new Dictionary<string, string?>
                {
                    ["JwtSigningKeys:ActiveKeyId"] = ""
                },
                "Active JWT signing key id is required.");
        }

        [Fact]
        public async Task Startup_WithShortActiveJwtSigningKeySecret_FailsFast()
        {
            await AssertStartupFailsAsync(
                new Dictionary<string, string?>
                {
                    ["JwtSigningKeys:Keys:0:Secret"] = "too-short"
                },
                "secret must be at least");
        }

        [Fact]
        public async Task Startup_WithMissingCsrfHeaderName_FailsFast()
        {
            await AssertStartupFailsAsync(
                new Dictionary<string, string?>
                {
                    ["Csrf:HeaderName"] = ""
                },
                "CSRF header name is required.");
        }

        [Fact]
        public async Task Startup_WithInvalidRefreshTokenExpiration_FailsFast()
        {
            await AssertStartupFailsAsync(
                new Dictionary<string, string?>
                {
                    ["RefreshTokens:LifetimeDays"] = "0"
                },
                "Refresh token expiration days must be greater than zero.");
        }

        private static async Task AssertStartupFailsAsync(
            Dictionary<string, string?> configurationOverrides,
            string expectedMessagePart)
        {
            await using var factory = new CorporateStarterApiFactory(configurationOverrides);

            await factory.InitializeAsync();

            var exception = Assert.ThrowsAny<Exception>(() =>
            {
                using var scope = factory.Services.CreateScope();

                _ = scope.ServiceProvider.GetRequiredService<IOptions<JwtSigningKeyOptions>>().Value;
                _ = scope.ServiceProvider.GetRequiredService<IOptions<RefreshTokenOptions>>().Value;
                _ = scope.ServiceProvider.GetRequiredService<IOptions<CsrfOptions>>().Value;
            });

            Assert.Contains(
                expectedMessagePart,
                exception.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
