using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Core.Entities.MasterData;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Cities;
using CorporateStarter.Tests.Integration.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CorporateStarter.Tests.Integration.MasterData;

public sealed class CityTableTests(CorporateStarterApiFactory factory) : IClassFixture<CorporateStarterApiFactory>
{
    [Fact]
    public async Task Table_endpoints_preserve_city_CRUD_permissions_and_soft_delete()
    {
        using var app = factory.WithWebHostBuilder(builder => builder
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureTestServices(services => services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "CityTableTest";
                options.DefaultChallengeScheme = "CityTableTest";
                options.DefaultForbidScheme = "CityTableTest";
            }).AddScheme<AuthenticationSchemeOptions, TableTestAuthentication>("CityTableTest", _ => { })));
        using var client = app.CreateClient();
        Guid activeId, inactiveId, countryId;
        var prefix = "City-table-" + Guid.NewGuid().ToString("N");
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            client.DefaultRequestHeaders.Add("X-Test-User", (await db.Users.Select(x => x.Id).FirstAsync()).ToString());
            var country = new Country { Id = Guid.NewGuid(), Code = "ZX", Name = prefix + " Country", IsActive = true };
            db.Countries.Add(country);
            var a = new City { Id = Guid.NewGuid(), Name = prefix + " Alpha", CountryId = country.Id, Region = "Team lead", IsActive = true };
            var b = new City { Id = Guid.NewGuid(), Name = prefix + " Beta", CountryId = country.Id, Region = "Archive", IsActive = false };
            db.Cities.AddRange(a, b);
            await db.SaveChangesAsync();
            activeId = a.Id; inactiveId = b.Id; countryId = country.Id;
        }
        void Permissions(params string[] permissions)
        {
            client.DefaultRequestHeaders.Remove("X-Test-Permissions");
            client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(',', permissions));
        }
        var query = new TableRequest { PageSize = 1, Sorts = [new("name")], Filters = [new("name", "contains", JsonSerializer.SerializeToElement(prefix))] };
        Permissions();
        foreach (var endpoint in new[] { "query", "find", "filter-values" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Cities/" + endpoint, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/Cities/capabilities")).StatusCode);
        Permissions(AppPermissions.CitiesRead);
        var capabilities = (await client.GetFromJsonAsync<TableCapabilities>("/api/Cities/capabilities"))!;
        Assert.True(capabilities.ViewInactive);
        Assert.False(capabilities.CanUpdate);
        Assert.False(capabilities.CanDelete);
        var page = await Post<PagedResult<CityListItemDto>>("query", query);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(activeId, Assert.Single(page.Items).Id);
        Assert.Equal(countryId, page.Items[0].CountryId);
        Assert.Equal("ZX", page.Items[0].CountryCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Cities/country-options", new LookupRequest())).StatusCode);
        query.Sorts = [new("countryName"), new("name")];
        Assert.Equal(activeId, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = prefix + " Country" })).Id);
        query.Sorts = [new("name")];
        Assert.Equal(2, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, LocateId = inactiveId })).PageNumber);
        Assert.Equal(activeId, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = "Team lead" })).Id);
        Assert.Equal(activeId, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = prefix, AfterId = inactiveId })).Id);
        Assert.Equal(new[] { "Active", "Inactive" }, await Post<string[]>("filter-values", new TableValuesRequest { Query = query, Field = "isActive" }));
        query.Filters.Add(new("isActive", "in", Values: [JsonSerializer.SerializeToElement(false)]));
        Assert.Equal(inactiveId, Assert.Single((await Post<PagedResult<CityListItemDto>>("query", query)).Items).Id);
        query.Filters[1] = new("isActive", "in", Values: []);
        Assert.Empty((await Post<PagedResult<CityListItemDto>>("query", query)).Items);
        query.Filters.RemoveAt(1);
        query.PageNumber = 99;
        Assert.Equal(2, (await Post<PagedResult<CityListItemDto>>("query", query)).Page);
        query.PageSize = 101;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Cities/query", query)).StatusCode);
        query.PageSize = 1;
        query.Sorts = [new("name; DROP TABLE Cities")];
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Cities/query", query)).StatusCode);
        Permissions(AppPermissions.CitiesRead, AppPermissions.CitiesUpdate);
        var options = await Post<LookupOption[]>("country-options", new LookupRequest { Text = prefix, Limit = 1 });
        Assert.Equal(countryId, Assert.Single(options).Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Cities/country-options", new LookupRequest { Limit = 51 })).StatusCode);
        var updated = await client.PutAsJsonAsync($"/api/Cities/{inactiveId}", new UpdateCityRequest { Name = prefix + " Beta", Region = "Archive", CountryId = countryId, IsActive = true });
        updated.EnsureSuccessStatusCode();
        Assert.True((await updated.Content.ReadFromJsonAsync<CityDetailsDto>())!.IsActive);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/Cities/{activeId}")).StatusCode);
        Permissions(AppPermissions.CitiesRead, AppPermissions.CitiesDelete);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/Cities/{activeId}")).StatusCode);
        Assert.False((await client.GetFromJsonAsync<CityDetailsDto>($"/api/Cities/{activeId}"))!.IsActive);
        Permissions(AppPermissions.CitiesRead, AppPermissions.CitiesCreate);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Cities", new CreateCityRequest { Name = new string('X', 151), CountryId = countryId })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/Cities", new CreateCityRequest { Name = prefix + " Beta", Region = "Archive", CountryId = countryId })).StatusCode);

        async Task<T> Post<T>(string endpoint, object body)
        {
            var response = await client.PostAsJsonAsync("/api/Cities/" + endpoint, body);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }
    }
}
