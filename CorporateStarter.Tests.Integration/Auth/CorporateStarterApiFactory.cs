using CorporateStarter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace CorporateStarter.Tests.Integration.Auth;

public sealed class CorporateStarterApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly Dictionary<string, string?> _configurationOverrides;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("corporatestarter_tests")
        .WithUsername("corporatestarter")
        .WithPassword("corporatestarter")
        .Build();

    public CorporateStarterApiFactory()
    : this(null)
    {
    }

    internal CorporateStarterApiFactory(
        Dictionary<string, string?>? configurationOverrides)
    {
        _configurationOverrides = configurationOverrides ?? [];
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),

                ["Jwt:Issuer"] = "CorporateStarter",
                ["Jwt:Audience"] = "CorporateStarter.Web",
                ["Jwt:ExpirationMinutes"] = "5",
                ["Jwt:Secret"] = "LEGACY_TEST_SECRET_32_BYTES_MINIMUM",

                ["JwtSigningKeys:ActiveKeyId"] = "test-2026-08",
                ["JwtSigningKeys:Keys:0:KeyId"] = "test-2026-08",
                ["JwtSigningKeys:Keys:0:Secret"] = "TEST_SIGNING_SECRET_32_BYTES_MINIMUM_VALUE",
                ["JwtSigningKeys:Keys:0:IsEnabled"] = "true",

                ["RefreshTokens:LifetimeDays"] = "7",
                ["RefreshTokens:CookieName"] = "__Host-corporate_starter_refresh",
                ["RefreshTokens:SecureCookie"] = "false",
                ["RefreshTokens:SameSite"] = "Strict",

                ["Csrf:CookieName"] = "__Host-corporate_starter_csrf",
                ["Csrf:HeaderName"] = "X-CSRF-TOKEN",
                ["Csrf:SecureCookie"] = "false",
                ["Csrf:SameSite"] = "Strict",

                ["LoginAttempts:WindowMinutes"] = "15",
                ["LoginAttempts:MaxFailedAttempts"] = "3",
                ["LoginAttempts:LockoutMinutes"] = "15",
                ["LoginAttempts:ProgressiveDelayBaseMilliseconds"] = "1",
                ["LoginAttempts:MaxProgressiveDelayMilliseconds"] = "5",

                ["InitialAdmin:Login"] = "admin",
                ["InitialAdmin:Email"] = "admin@corporatestarter.test",
                ["InitialAdmin:Password"] = "Admin123!ChangeMe"
            });
            config.AddInMemoryCollection(_configurationOverrides);
        });

        builder.UseEnvironment("Testing");
    }
}