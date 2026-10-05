using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Tests.Integration.MasterData;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Permissions;
using CorporateStarter.Tests.Integration.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CorporateStarter.Tests.Integration.Security;

public sealed class PermissionTableTests(CorporateStarterApiFactory factory) : IClassFixture<CorporateStarterApiFactory>
{
    [Fact]
    public async Task Catalog_requires_read_permission_and_supports_query_find_and_filters()
    {
        using var app = factory.WithWebHostBuilder(builder => builder
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureTestServices(services => services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "PermissionTableTest";
                options.DefaultChallengeScheme = "PermissionTableTest";
                options.DefaultForbidScheme = "PermissionTableTest";
            }).AddScheme<AuthenticationSchemeOptions, TableTestAuthentication>("PermissionTableTest", _ => { })));
        using var client = app.CreateClient();
        Guid activeId, inactiveId;
        var prefix = "Permission-table-" + Guid.NewGuid().ToString("N");
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            client.DefaultRequestHeaders.Add("X-Test-User", (await db.Users.Select(x => x.Id).FirstAsync()).ToString());
            var a = new Permission { Id = Guid.NewGuid(), Code = prefix + ".alpha", Group = prefix, Name = "Alpha", Description = "Team lead", IsActive = true };
            var b = new Permission { Id = Guid.NewGuid(), Code = prefix + ".beta", Group = prefix, Name = "Beta", Description = "Archive", IsActive = false };
            db.Permissions.AddRange(a, b);
            await db.SaveChangesAsync();
            activeId = a.Id; inactiveId = b.Id;
        }
        void Permissions(params string[] permissions)
        {
            client.DefaultRequestHeaders.Remove("X-Test-Permissions");
            client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(',', permissions));
        }
        var query = new TableRequest { PageSize = 1, Sorts = [new("name")], Filters = [new("group", "eq", JsonSerializer.SerializeToElement(prefix))] };
        Permissions();
        foreach (var endpoint in new[] { "query", "find", "filter-values" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Permissions/" + endpoint, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/Permissions/capabilities")).StatusCode);
        Permissions(AppPermissions.PermissionsRead);
        var capabilities = (await client.GetFromJsonAsync<TableCapabilities>("/api/Permissions/capabilities"))!;
        Assert.True(capabilities.ViewInactive);
        Assert.False(capabilities.CanCreate);
        Assert.False(capabilities.CanRestore);
        Assert.False(capabilities.CanUpdate);
        Assert.False(capabilities.CanDelete);
        var page = await Post<PagedResult<PermissionListItemDto>>("query", query);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(activeId, Assert.Single(page.Items).Id);
        Assert.Equal(2, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, LocateId = inactiveId })).PageNumber);
        Assert.Equal(activeId, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = "Team lead" })).Id);
        Assert.Equal(activeId, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = prefix, AfterId = inactiveId })).Id);
        Assert.Equal(new[] { "Active", "Inactive" }, await Post<string[]>("filter-values", new TableValuesRequest { Query = query, Field = "isActive" }));
        query.Filters.Add(new("isActive", "in", Values: [JsonSerializer.SerializeToElement(false)]));
        Assert.Equal(inactiveId, Assert.Single((await Post<PagedResult<PermissionListItemDto>>("query", query)).Items).Id);
        query.Filters[1] = new("isActive", "in", Values: []);
        Assert.Empty((await Post<PagedResult<PermissionListItemDto>>("query", query)).Items);
        query.Filters.RemoveAt(1);
        query.PageNumber = 99;
        Assert.Equal(2, (await Post<PagedResult<PermissionListItemDto>>("query", query)).Page);
        query.PageSize = 101;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Permissions/query", query)).StatusCode);
        query.PageSize = 1;
        query.Sorts = [new("name; DROP TABLE Positions")];
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Permissions/query", query)).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsJsonAsync("/api/Permissions", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/Permissions/{activeId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/Permissions/{activeId}", new { })).StatusCode);

        async Task<T> Post<T>(string endpoint, object body)
        {
            var response = await client.PostAsJsonAsync("/api/Permissions/" + endpoint, body);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }
    }
}
