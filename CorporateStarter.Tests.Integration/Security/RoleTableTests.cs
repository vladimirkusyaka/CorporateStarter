using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Tests.Integration.MasterData;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.Roles;
using CorporateStarter.Tests.Integration.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CorporateStarter.Tests.Integration.Security;

public sealed class RoleTableTests(CorporateStarterApiFactory factory) : IClassFixture<CorporateStarterApiFactory>
{
    [Fact]
    public async Task Role_table_and_CRUD_preserve_permissions_and_system_roles()
    {
        using var app = factory.WithWebHostBuilder(builder => builder
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureTestServices(services => services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "RoleTableTest";
                options.DefaultChallengeScheme = "RoleTableTest";
                options.DefaultForbidScheme = "RoleTableTest";
            }).AddScheme<AuthenticationSchemeOptions, TableTestAuthentication>("RoleTableTest", _ => { })));
        using var client = app.CreateClient();
        Guid activeId, inactiveId;
        var prefix = "Role-table-" + Guid.NewGuid().ToString("N");
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            client.DefaultRequestHeaders.Add("X-Test-User", (await db.Users.Select(x => x.Id).FirstAsync()).ToString());
            var a = new Role { Id = Guid.NewGuid(), Name = prefix + " Alpha", Description = "Team lead", IsActive = true };
            var b = new Role { Id = Guid.NewGuid(), Name = prefix + " Beta", Description = "Archive", IsActive = false };
            db.Roles.AddRange(a, b);
            await db.SaveChangesAsync();
            activeId = a.Id; inactiveId = b.Id;
        }
        void Permissions(params string[] permissions)
        {
            client.DefaultRequestHeaders.Remove("X-Test-Permissions");
            client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(',', permissions));
        }
        var query = new TableRequest { PageSize = 1, Sorts = [new("name")], Filters = [new("name", "contains", JsonSerializer.SerializeToElement(prefix))] };
        Permissions();
        foreach (var endpoint in new[] { "query", "find", "filter-values" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Roles/" + endpoint, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/Roles/capabilities")).StatusCode);
        Permissions(AppPermissions.RolesRead);
        var capabilities = (await client.GetFromJsonAsync<RoleCapabilities>("/api/Roles/capabilities"))!.Table;
        Assert.True(capabilities.ViewInactive);
        Assert.False(capabilities.CanCreate);
        Assert.False(capabilities.CanRestore);
        Assert.False(capabilities.CanUpdate);
        Assert.False(capabilities.CanDelete);
        var page = await Post<PagedResult<RoleListItemDto>>("query", query);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(activeId, Assert.Single(page.Items).Id);
        Assert.Equal(2, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, LocateId = inactiveId })).PageNumber);
        Assert.Equal(activeId, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = "Team lead" })).Id);
        Assert.Equal(activeId, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = prefix, AfterId = inactiveId })).Id);
        Assert.Equal(new[] { "Active", "Inactive" }, await Post<string[]>("filter-values", new TableValuesRequest { Query = query, Field = "isActive" }));
        query.Filters.Add(new("isActive", "in", Values: [JsonSerializer.SerializeToElement(false)]));
        Assert.Equal(inactiveId, Assert.Single((await Post<PagedResult<RoleListItemDto>>("query", query)).Items).Id);
        query.Filters[1] = new("isActive", "in", Values: []);
        Assert.Empty((await Post<PagedResult<RoleListItemDto>>("query", query)).Items);
        query.Filters.RemoveAt(1);
        query.PageNumber = 99;
        Assert.Equal(2, (await Post<PagedResult<RoleListItemDto>>("query", query)).Page);
        query.PageSize = 101;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Roles/query", query)).StatusCode);
        query.PageSize = 1;
        query.Sorts = [new("name; DROP TABLE Positions")];
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Roles/query", query)).StatusCode);
        Permissions(AppPermissions.RolesRead, AppPermissions.RolesCreate, AppPermissions.RolesUpdate,
            AppPermissions.RolesDelete, AppPermissions.RolesManagePermissions);
        var options = (await client.GetFromJsonAsync<CorporateStarter.Shared.Dtos.Permissions.PermissionListItemDto[]>("/api/Roles/permission-options"))!;
        var permissionId = options.First(x => x.IsActive).Id;
        var created = await client.PostAsJsonAsync("/api/Roles", new CreateRoleRequest { Name = prefix + " Created", PermissionIds = [permissionId] });
        created.EnsureSuccessStatusCode();
        using var command = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = command.RootElement.GetProperty("value").GetGuid();
        Permissions(AppPermissions.RolesRead, AppPermissions.RolesUpdate);
        var update = await client.PutAsJsonAsync($"/api/Roles/{id}", new UpdateRoleRequest { Name = prefix + " Renamed" });
        update.EnsureSuccessStatusCode();
        var details = (await client.GetFromJsonAsync<RoleDetailsDto>($"/api/Roles/{id}"))!;
        Assert.Equal(permissionId, Assert.Single(details.Permissions).Id);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/Roles/{id}", new UpdateRoleRequest { Name = details.Name, PermissionIds = [] })).StatusCode);
        Permissions(AppPermissions.RolesRead, AppPermissions.RolesUpdate, AppPermissions.RolesDelete, AppPermissions.RolesManagePermissions);
        (await client.PutAsJsonAsync($"/api/Roles/{id}", new UpdateRoleRequest { Name = details.Name, PermissionIds = [] })).EnsureSuccessStatusCode();
        Assert.Empty((await client.GetFromJsonAsync<RoleDetailsDto>($"/api/Roles/{id}"))!.Permissions);
        (await client.DeleteAsync($"/api/Roles/{id}")).EnsureSuccessStatusCode();
        Assert.False((await client.GetFromJsonAsync<RoleDetailsDto>($"/api/Roles/{id}"))!.IsActive);
        var roles = (await client.GetFromJsonAsync<RoleListItemDto[]>("/api/Roles"))!;
        var system = roles.First(x => x.IsSystemRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/Roles/{system.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/Roles/{system.Id}", new UpdateRoleRequest { Name = "Renamed system" })).StatusCode);

        async Task<T> Post<T>(string endpoint, object body)
        {
            var response = await client.PostAsJsonAsync("/api/Roles/" + endpoint, body);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }
    }
}
