using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CorporateStarter.Api.Controllers.MasterData;
using CorporateStarter.Api.Security;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.MasterData.Countries.Handlers;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Infrastructure.Persistence.Repositories.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Tests.Integration.MasterData;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
namespace CorporateStarter.Tests.Integration.Security;

public sealed class EntityAccessTests
{
    [Theory]
    [InlineData("Countries")]
    [InlineData("Cities")]
    [InlineData("Positions")]
    [InlineData("Companies")]
    [InlineData("Persons")]
    [InlineData("Users")]
    [InlineData("Roles")]
    [InlineData("Permissions")]
    public async Task Capabilities_require_read_and_do_not_infer_inactive_or_restore_from_update(string resource)
    {
        using var host = new HostBuilder().ConfigureWebHost(b => b.UseTestServer().ConfigureServices(s =>
        {
            s.AddLogging(); s.AddHttpContextAccessor();
            s.AddScoped<IEntityAccess, EntityAccess>(); s.AddScoped<ICountryAccess, CountryAccess>();
            s.AddControllers().AddApplicationPart(typeof(CountriesController).Assembly);
            s.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<CreateCountryCommandHandler>());
            s.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TableTestAuthentication>("Test", _ => { });
            s.AddAuthorization(o => { foreach (var p in AppPermissions.All) o.AddPolicy(p.Code, policy => policy.RequireClaim("permission", p.Code)); });
        }).Configure(app => { app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseEndpoints(e => e.MapControllers()); })).Start();
        using var client = host.GetTestClient();
        var path = "/api/" + resource + "/capabilities";
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
        var code = resource.ToLowerInvariant();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", code + ".read," + code + ".update");
        var caps = await Read();
        Assert.False(caps.ViewInactive); Assert.False(caps.CanRestore);
        client.DefaultRequestHeaders.Remove("X-Test-Permissions");
        client.DefaultRequestHeaders.Add("X-Test-Permissions", code + ".read," + code + ".view_inactive" + (code == "permissions" ? "" : "," + code + ".restore"));
        caps = await Read();
        Assert.True(caps.ViewInactive); Assert.Equal(code != "permissions", caps.CanRestore);
        Assert.False(caps.CanUpdate); Assert.False(caps.CanDelete);
        async Task<TableCapabilities> Read()
        {
            var response = await client.GetAsync(path); response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var value = json.RootElement;
            if (value.TryGetProperty("table", out var table)) value = table;
            return value.Deserialize<TableCapabilities>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Inactive_user_and_role_creation_cannot_bypass_status_rights(bool view, bool delete)
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused").Options);
        var access = new Access(new(true, true, delete, view, true));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new RoleWriteRepository(db, access)
            .AddAsync(new() { Name = "Inactive role", IsActive = false }, default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new UserWriteRepository(db, access)
            .AddAsync(new() { Login = "user", IsActive = false }, "hash", default));
        Assert.Empty(db.ChangeTracker.Entries());
    }
    private sealed class Access(TableCapabilities caps) : IEntityAccess
    { public TableCapabilities For(string resource) => caps; }
}
