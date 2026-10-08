using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Core.Entities.Directory;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Companies;
using CorporateStarter.Tests.Integration.Auth;
using CorporateStarter.Tests.Integration.MasterData;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
namespace CorporateStarter.Tests.Integration.Directory;

public sealed class CompanyTableTests(CorporateStarterApiFactory factory) : IClassFixture<CorporateStarterApiFactory>
{
    [Fact]
    public async Task Query_includes_companies_without_city_and_supports_find_filters_and_clear_city()
    {
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureLogging(log => log.ClearProviders())
            .ConfigureTestServices(services => services.AddAuthentication(options =>
            { options.DefaultAuthenticateScheme = "CompanyTableTest"; options.DefaultChallengeScheme = "CompanyTableTest"; options.DefaultForbidScheme = "CompanyTableTest"; })
                .AddScheme<AuthenticationSchemeOptions, TableTestAuthentication>("CompanyTableTest", _ => { })));
        using var client = app.CreateClient();
        var prefix = "company-" + Guid.NewGuid().ToString("N"); Guid aId, bId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            client.DefaultRequestHeaders.Add("X-Test-User", (await db.Users.Select(x => x.Id).FirstAsync()).ToString());
            var country = new CorporateStarter.Core.Entities.MasterData.Country { Id = Guid.NewGuid(), Code = "XZ", Name = prefix, IsActive = true };
            var city = new CorporateStarter.Core.Entities.MasterData.City { Id = Guid.NewGuid(), Name = prefix, CountryId = country.Id, IsActive = true };
            db.Countries.Add(country); db.Cities.Add(city);
            var a = new Company { Id = Guid.NewGuid(), Name = prefix + " A", IsActive = true, CityId = null };
            var b = new Company { Id = Guid.NewGuid(), Name = prefix + " B", IsActive = false, CityId = city.Id };
            db.Companies.AddRange(a, b); await db.SaveChangesAsync(); aId = a.Id; bId = b.Id;
        }
        client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(',', AppPermissions.CompaniesRead, AppPermissions.CompaniesUpdate));
        var query = new TableRequest { PageSize = 1, Filters = [new("name", "contains", JsonSerializer.SerializeToElement(prefix))] };
        var page = await Post<PagedResult<CompanyListItemDto>>("query", query);
        Assert.Equal(2, page.TotalCount); Assert.Equal(aId, Assert.Single(page.Items).Id); Assert.Null(page.Items[0].CityId);
        Assert.Equal(2, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, LocateId = bId })).PageNumber);
        Assert.Equal(bId, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = prefix + " B" })).Id);
        query.Filters.Add(new("isActive", "in", Values: [JsonSerializer.SerializeToElement(false)]));
        var row = Assert.Single((await Post<PagedResult<CompanyListItemDto>>("query", query)).Items);
        Assert.Equal(prefix, row.CityName); Assert.Equal(prefix, row.CountryName);
        Assert.Equal(new[] { "Active", "Inactive" }, await Post<string[]>("filter-values", new TableValuesRequest { Query = query, Field = "isActive" }));
        Assert.Single(await Post<LookupOption[]>("city-options", new LookupRequest { Text = prefix }));
        var response = await client.PutAsJsonAsync("/api/Companies/" + bId, new UpdateCompanyRequest { Name = prefix + " B", CityId = null, IsActive = true });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Null((await response.Content.ReadFromJsonAsync<CompanyDetailsDto>())!.CityId);
        query.PageSize = 101; Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Companies/query", query)).StatusCode);
        async Task<T> Post<T>(string route, object body)
        { var result = await client.PostAsJsonAsync("/api/Companies/" + route, body); Assert.Equal(HttpStatusCode.OK, result.StatusCode); return (await result.Content.ReadFromJsonAsync<T>())!; }
    }
}
