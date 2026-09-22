using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Core.Entities.MasterData;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
using CorporateStarter.Tests.Integration.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace CorporateStarter.Tests.Integration.MasterData;

public sealed class CountryTableTests(CorporateStarterApiFactory factory) : IClassFixture<CorporateStarterApiFactory>
{
    [Fact]
    public async Task Paging_Search_Filters_And_Inactive_Permissions_Are_Enforced_On_Postgres()
    {
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.ClearProviders()).ConfigureTestServices(services =>
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TableTest";
                options.DefaultChallengeScheme = "TableTest";
                options.DefaultForbidScheme = "TableTest";
            }).AddScheme<AuthenticationSchemeOptions, TableTestAuthentication>("TableTest", _ => { })));
        using var client = app.CreateClient();
        Guid activeId, inactiveId, userId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            userId = await db.Users.Select(x => x.Id).FirstAsync();
            var first = new Country { Id = Guid.NewGuid(), Code = "XA", Name = "Test-table Alpha", IsActive = true };
            var second = new Country { Id = Guid.NewGuid(), Code = "XB", Name = "Test-table Beta", IsActive = false };
            var third = new Country { Id = Guid.NewGuid(), Code = "XC", Name = "Test-table Gamma", IsActive = true };
            db.Countries.AddRange(first, second, third); await db.SaveChangesAsync();
            activeId = first.Id; inactiveId = second.Id;
        }
        client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
        void Permissions(params string[] codes)
        {
            client.DefaultRequestHeaders.Remove("X-Test-Permissions");
            client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(',', codes));
        }
        var query = new TableRequest
        {
            PageSize = 1,
            Sorts = [new("name")],
            Filters = [new("name", "contains", JsonSerializer.SerializeToElement("Test-table"))]
        };
        Permissions(AppPermissions.CountriesRead, AppPermissions.CountriesUpdate, AppPermissions.CountriesDelete);
        var page = await Post<PagedResult<CountryListItemDto>>("query", query);
        Assert.Equal(2, page.TotalCount); Assert.Single(page.Items); Assert.Equal(activeId, page.Items[0].Id);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/Countries/{inactiveId}")).StatusCode);
        Assert.DoesNotContain((await client.GetFromJsonAsync<CountryListItemDto[]>("/api/Countries"))!, x => !x.IsActive);
        var found = await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = "Gamma" });
        Assert.Equal(2, found.PageNumber);
        Assert.Null((await Post<TableFindResult>("find", new TableFindRequest { Query = query, LocateId = inactiveId })).Id);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Countries/filter-values", new TableValuesRequest { Query = query, Field = "isActive" })).StatusCode);
        query.Sorts = [new("isActive")]; Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Countries/query", query)).StatusCode);
        query.Sorts = [new("name")]; query.Filters.Add(new("isActive", "in", Values: [JsonSerializer.SerializeToElement(false)]));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Countries/query", query)).StatusCode); query.Filters.RemoveAt(1);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/Countries/{inactiveId}")).StatusCode);
        var update = new UpdateCountryRequest { Code = "XA", Name = "Test-table Alpha changed" };
        var updated = await client.PutAsJsonAsync($"/api/Countries/{activeId}", update); updated.EnsureSuccessStatusCode();
        Assert.True((await updated.Content.ReadFromJsonAsync<CountryDetailsDto>())!.IsActive);
        update.IsActive = false;
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/Countries/{activeId}", update)).StatusCode);

        Permissions(AppPermissions.CountriesRead, AppPermissions.CountriesUpdate, AppPermissions.CountriesViewInactive);
        page = await Post<PagedResult<CountryListItemDto>>("query", query); Assert.Equal(3, page.TotalCount);
        Assert.Equal(3, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = "Gamma" })).PageNumber);
        var lastMatch = await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = "Gamma" });
        Assert.Equal(activeId, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = "Test-table", AfterId = lastMatch.Id })).Id);
        query.Filters.Add(new("isActive", "in", Values: [JsonSerializer.SerializeToElement(false)]));
        page = await Post<PagedResult<CountryListItemDto>>("query", query);
        Assert.Equal(1, page.TotalCount); Assert.Equal(inactiveId, page.Items[0].Id);
        query.Filters[1] = new("isActive", "in", Values: []);
        Assert.Equal(0, (await Post<PagedResult<CountryListItemDto>>("query", query)).TotalCount);
        query.Filters.RemoveAt(1);
        Assert.Equal(new[] { "Active", "Inactive" }, await Post<string[]>("filter-values", new TableValuesRequest { Query = query, Field = "isActive" }));
        var inactiveUpdate = new UpdateCountryRequest { Code = "XB", Name = "Test-table Beta", IsActive = true };
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/Countries/{inactiveId}", inactiveUpdate)).StatusCode);
        inactiveUpdate.IsActive = null;
        var unchanged = await client.PutAsJsonAsync($"/api/Countries/{inactiveId}", inactiveUpdate); unchanged.EnsureSuccessStatusCode();
        Assert.False((await unchanged.Content.ReadFromJsonAsync<CountryDetailsDto>())!.IsActive);
        Permissions(AppPermissions.CountriesRead, AppPermissions.CountriesUpdate, AppPermissions.CountriesViewInactive, AppPermissions.CountriesRestore);
        inactiveUpdate.IsActive = true; (await client.PutAsJsonAsync($"/api/Countries/{inactiveId}", inactiveUpdate)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/Countries/{activeId}")).StatusCode);
        Permissions(AppPermissions.CountriesRead, AppPermissions.CountriesDelete);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/Countries/{activeId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/Countries/{activeId}")).StatusCode);
        query.PageNumber = 999; page = await Post<PagedResult<CountryListItemDto>>("query", query); Assert.Equal(page.TotalCount, page.Page);
        query.PageSize = 101; Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Countries/query", query)).StatusCode);
        query.PageSize = 1; query.Sorts = [new("name; DROP TABLE Countries")];
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Countries/query", query)).StatusCode);
        query.Sorts = [new("name", "desc"), new("code", "asc")]; query.PageNumber = 1;
        Assert.Equal("XC", (await Post<PagedResult<CountryListItemDto>>("query", query)).Items[0].Code);
        query.Filters = [new("name", "contains", JsonSerializer.SerializeToElement("%' OR TRUE --"))];
        Assert.Equal(0, (await Post<PagedResult<CountryListItemDto>>("query", query)).TotalCount);
        Permissions(); Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Countries/query", new TableRequest())).StatusCode);

        async Task<T> Post<T>(string route, object body)
        {
            var response = await client.PostAsJsonAsync("/api/Countries/" + route, body);
            Assert.True(response.IsSuccessStatusCode, $"{route}: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }
    }
}

// Test host only: supplies claims to exercise the real authorization policies and real PostgreSQL repositories.
public sealed class TableTestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = Request.Headers["X-Test-Permissions"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => new Claim("permission", x)).ToList();
        claims.Add(new(ClaimTypes.NameIdentifier, Request.Headers["X-Test-User"].ToString()));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
    }
}

