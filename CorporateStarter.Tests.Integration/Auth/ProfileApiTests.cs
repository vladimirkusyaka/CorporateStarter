using System.Net;
using System.Net.Http.Json;
using CorporateStarter.Api.Controllers;
using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Shared.Dtos.Auth;
using CorporateStarter.Tests.Integration.MasterData;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
namespace CorporateStarter.Tests.Integration.Auth;

public sealed class ProfileApiTests
{
    [Fact]
    public async Task Self_service_uses_claim_id_without_users_permissions_and_validates_settings()
    {
        var service = new Service();
        using var host = new HostBuilder().ConfigureWebHost(b => b.UseTestServer().ConfigureServices(s =>
        {
            s.AddLogging(); s.AddControllers().AddApplicationPart(typeof(MyProfileController).Assembly);
            s.AddSingleton<IMyProfileService>(service);
            s.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TableTestAuthentication>("Test", _ => { });
            s.AddAuthorization();
        }).Configure(a => { a.UseRouting(); a.UseAuthentication(); a.UseAuthorization(); a.UseEndpoints(e => e.MapControllers()); })).Start();
        using var client = host.GetTestClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profile")).StatusCode);
        var id = Guid.NewGuid(); client.DefaultRequestHeaders.Add("X-Test-User", id.ToString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/profile")).StatusCode); Assert.Equal(id, service.LastId);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/profile/account", new { displayName = "Alice", userId = Guid.NewGuid(), isActive = false, roleIds = new[] { Guid.NewGuid() } })).StatusCode);
        Assert.Equal(id, service.LastId); Assert.Equal("Alice", service.Name);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/profile/appearance", new ProfileSettingsDto { PageSize = 21 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/profile/appearance", new ProfileSettingsDto { Language = "fr-FR" })).StatusCode);
        Assert.Equal(0, service.AppearanceWrites);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/profile/appearance", new ProfileSettingsDto { Theme = "Dark", PageSize = 50, Language = "de-DE" })).StatusCode);
        Assert.Equal(1, service.AppearanceWrites); Assert.Equal("de-DE", service.Language);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/profile/password", new ChangeMyPasswordRequest())).StatusCode);
        service.Error = "profile.current_password_invalid";
        var rejected = await client.PostAsJsonAsync("/api/profile/password", new ChangeMyPasswordRequest { CurrentPassword = "old", NewPassword = "new" });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode); Assert.Contains(service.Error, await rejected.Content.ReadAsStringAsync());
        Assert.Equal(id, service.LastId);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/profile/" + Guid.NewGuid() + "/account", new { })).StatusCode);
    }
    private sealed class Service : IMyProfileService
    {
        public Guid LastId; public string? Name, Error, Language; public int AppearanceWrites;
        public Task<MyProfileDto?> GetAsync(Guid id, CancellationToken ct) { LastId = id; return Task.FromResult<MyProfileDto?>(new() { Account = new() { Id = id, Login = "alice" } }); }
        public Task<bool> UpdateAccountAsync(Guid id, UpdateMyAccountRequest r, CancellationToken ct) { LastId = id; Name = r.DisplayName; return Task.FromResult(true); }
        public Task<bool> UpdateAppearanceAsync(Guid id, ProfileSettingsDto r, CancellationToken ct) { LastId = id; Language = r.Language; AppearanceWrites++; return Task.FromResult(true); }
        public Task<string?> ChangePasswordAsync(Guid id, ChangeMyPasswordRequest r, CancellationToken ct) { LastId = id; return Task.FromResult(Error); }
    }
}
