using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Core.Entities.MasterData;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Positions;
using CorporateStarter.Tests.Integration.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CorporateStarter.Tests.Integration.MasterData;

public sealed class PositionTableTests(CorporateStarterApiFactory factory) : IClassFixture<CorporateStarterApiFactory>
{
    [Fact]
    public async Task Table_endpoints_preserve_position_CRUD_permissions_and_soft_delete()
    {
        using var app = factory.WithWebHostBuilder(builder => builder
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureTestServices(services => services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "PositionTableTest";
                options.DefaultChallengeScheme = "PositionTableTest";
                options.DefaultForbidScheme = "PositionTableTest";
            }).AddScheme<AuthenticationSchemeOptions, TableTestAuthentication>("PositionTableTest", _ => { })));
        using var client = app.CreateClient();
        Guid activeId, inactiveId;
        var prefix = "Position-table-" + Guid.NewGuid().ToString("N");
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            client.DefaultRequestHeaders.Add("X-Test-User", (await db.Users.Select(x => x.Id).FirstAsync()).ToString());
            var a = new Position { Id = Guid.NewGuid(), Name = prefix + " Alpha", Description = "Team lead", IsActive = true };
            var b = new Position { Id = Guid.NewGuid(), Name = prefix + " Beta", Description = "Archive", IsActive = false };
            db.Positions.AddRange(a, b);
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
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Positions/" + endpoint, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/Positions/capabilities")).StatusCode);
        Permissions(AppPermissions.PositionsRead);
        var capabilities = (await client.GetFromJsonAsync<TableCapabilities>("/api/Positions/capabilities"))!;
        Assert.True(capabilities.ViewInactive);
        Assert.False(capabilities.CanUpdate);
        Assert.False(capabilities.CanDelete);
        var page = await Post<PagedResult<PositionListItemDto>>("query", query);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(activeId, Assert.Single(page.Items).Id);
        Assert.Equal(2, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, LocateId = inactiveId })).PageNumber);
        Assert.Equal(activeId, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = "Team lead" })).Id);
        Assert.Equal(activeId, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = prefix, AfterId = inactiveId })).Id);
        Assert.Equal(new[] { "Active", "Inactive" }, await Post<string[]>("filter-values", new TableValuesRequest { Query = query, Field = "isActive" }));
        query.Filters.Add(new("isActive", "in", Values: [JsonSerializer.SerializeToElement(false)]));
        Assert.Equal(inactiveId, Assert.Single((await Post<PagedResult<PositionListItemDto>>("query", query)).Items).Id);
        query.Filters[1] = new("isActive", "in", Values: []);
        Assert.Empty((await Post<PagedResult<PositionListItemDto>>("query", query)).Items);
        query.Filters.RemoveAt(1);
        query.PageNumber = 99;
        Assert.Equal(2, (await Post<PagedResult<PositionListItemDto>>("query", query)).Page);
        query.PageSize = 101;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Positions/query", query)).StatusCode);
        query.PageSize = 1;
        query.Sorts = [new("name; DROP TABLE Positions")];
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Positions/query", query)).StatusCode);
        Permissions(AppPermissions.PositionsRead, AppPermissions.PositionsUpdate);
        var updated = await client.PutAsJsonAsync($"/api/Positions/{inactiveId}", new UpdatePositionRequest { Name = prefix + " Beta", IsActive = true });
        updated.EnsureSuccessStatusCode();
        Assert.True((await updated.Content.ReadFromJsonAsync<PositionDetailsDto>())!.IsActive);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/Positions/{activeId}")).StatusCode);
        Permissions(AppPermissions.PositionsRead, AppPermissions.PositionsDelete);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/Positions/{activeId}")).StatusCode);
        Assert.False((await client.GetFromJsonAsync<PositionDetailsDto>($"/api/Positions/{activeId}"))!.IsActive);
        Permissions(AppPermissions.PositionsRead, AppPermissions.PositionsCreate);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Positions", new CreatePositionRequest { Name = new string('X', 151) })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/Positions", new CreatePositionRequest { Name = prefix + " Beta" })).StatusCode);

        async Task<T> Post<T>(string endpoint, object body)
        {
            var response = await client.PostAsJsonAsync("/api/Positions/" + endpoint, body);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }
    }
}
