using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CorporateStarter.Api.Controllers;
using CorporateStarter.Application.Common.Interfaces;
using CorporateStarter.Application.Common.Interfaces.Repositories.Preferences;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Dtos.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
namespace CorporateStarter.Tests.Integration.Configuration;

public sealed class PreferencesApiTests
{
    [Fact]
    public async Task General_is_authenticated_connections_and_update_require_distinct_permissions()
    {
        var store = new Store();
        using var host = new HostBuilder().ConfigureWebHost(b => b.UseTestServer().ConfigureServices(s =>
        {
            s.AddLogging(); s.AddControllers().AddApplicationPart(typeof(PreferencesController).Assembly);
            s.AddSingleton<IPreferencesRepository>(store);
            s.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, Auth>("Test", _ => { });
            s.AddAuthorization(o =>
            {
                o.AddPolicy(AppPermissions.PreferencesUpdate, p => p.RequireClaim("permission", AppPermissions.PreferencesUpdate));
                o.AddPolicy(AppPermissions.PreferencesConnectionsRead, p => p.RequireClaim("permission", AppPermissions.PreferencesConnectionsRead));
            });
        }).Configure(a => { a.UseRouting(); a.UseAuthentication(); a.UseAuthorization(); a.UseEndpoints(e => e.MapControllers()); })).Start();
        using var client = host.GetTestClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/preferences")).StatusCode);
        client.DefaultRequestHeaders.Add("X-User", "reader");
        var info = await client.GetFromJsonAsync<PreferencesDto>("/api/preferences");
        Assert.False(info!.CanUpdate); Assert.False(info.CanViewConnections);
        Assert.NotNull(info.ApiBuildUtc);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/preferences/connections")).StatusCode);
        Assert.Equal(0, store.ConnectionReads);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/preferences/general", new { name = "No", revision = store.Revision })).StatusCode);
        client.DefaultRequestHeaders.Add("X-Rights", AppPermissions.PreferencesUpdate);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/preferences/connections")).StatusCode);
        foreach (var name in new[] { "   ", "bad\nname", new string('a', 101) })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/preferences/general", new { name, revision = store.Revision })).StatusCode);
        Assert.Equal(0, store.Writes);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/preferences/general", new { name = "New", revision = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/preferences/general", new { name = "New", revision = store.Revision })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Rights"); client.DefaultRequestHeaders.Add("X-Rights", AppPermissions.PreferencesConnectionsRead);
        var connections = await client.GetAsync("/api/preferences/connections");
        Assert.Equal(HttpStatusCode.OK, connections.StatusCode);
        Assert.DoesNotContain("password", await connections.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, store.ConnectionReads);
    }
    private sealed class Store : IPreferencesRepository
    {
        public Guid Revision = Guid.NewGuid(); public int Writes, ConnectionReads;
        public Task<GeneralPreferencesDto> GetAsync(CancellationToken ct) => Task.FromResult(new GeneralPreferencesDto("CorporateStarter", Revision));
        public Task<GeneralPreferencesDto?> UpdateAsync(UpdateGeneralPreferencesRequest request, CancellationToken ct)
        {
            if (request.Revision != Revision) return Task.FromResult<GeneralPreferencesDto?>(null);
            Writes++; Revision = Guid.NewGuid();
            return Task.FromResult<GeneralPreferencesDto?>(new(request.Name, Revision));
        }
        public ConnectionInfoDto GetConnections() { ConnectionReads++; return new("PostgreSQL", "db.internal", 5432, "application"); }
    }
    private sealed class Auth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("X-User")) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = Request.Headers["X-Rights"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => new Claim("permission", x));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }
}
