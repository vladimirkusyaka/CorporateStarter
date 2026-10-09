using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Core.Entities.Directory;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Persons;
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

public sealed class PersonTableTests(CorporateStarterApiFactory factory) : IClassFixture<CorporateStarterApiFactory>
{
    [Fact]
    public async Task Query_includes_unlinked_people_and_supports_find_filters_dates_and_clear_links()
    {
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureLogging(log => log.ClearProviders())
            .ConfigureTestServices(services => services.AddAuthentication(options =>
            { options.DefaultAuthenticateScheme = "PersonTableTest"; options.DefaultChallengeScheme = "PersonTableTest"; options.DefaultForbidScheme = "PersonTableTest"; })
                .AddScheme<AuthenticationSchemeOptions, TableTestAuthentication>("PersonTableTest", _ => { })));
        using var client = app.CreateClient();
        var prefix = "person-" + Guid.NewGuid().ToString("N"); Guid aId, bId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            client.DefaultRequestHeaders.Add("X-Test-User", (await db.Users.Select(x => x.Id).FirstAsync()).ToString());
            var company = new Company { Id = Guid.NewGuid(), Name = prefix, IsActive = true };
            var position = new CorporateStarter.Core.Entities.MasterData.Position { Id = Guid.NewGuid(), Name = prefix, IsActive = true };
            db.Companies.Add(company); db.Positions.Add(position);
            var a = new Person { Id = Guid.NewGuid(), FirstName = prefix + " A", IsActive = true };
            var b = new Person { Id = Guid.NewGuid(), FirstName = prefix + " B", IsActive = false, CompanyId = company.Id, PositionId = position.Id, DateOfBirth = new(1990, 2, 3) };
            db.People.AddRange(a, b); await db.SaveChangesAsync(); aId = a.Id; bId = b.Id;
        }
        client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(',', AppPermissions.PersonsRead, AppPermissions.PersonsUpdate));
        var query = new TableRequest { PageSize = 1, Filters = [new("firstName", "contains", JsonSerializer.SerializeToElement(prefix))] };
        var page = await Post<PagedResult<PersonListItemDto>>("query", query);
        Assert.Equal(2, page.TotalCount); Assert.Equal(aId, Assert.Single(page.Items).Id); Assert.Null(page.Items[0].CompanyId); Assert.Null(page.Items[0].PositionId);
        Assert.Equal(2, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, LocateId = bId })).PageNumber);
        Assert.Equal(bId, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = prefix + " B" })).Id);
        query.Filters.Add(new("isActive", "in", Values: [JsonSerializer.SerializeToElement(false)]));
        var row = Assert.Single((await Post<PagedResult<PersonListItemDto>>("query", query)).Items);
        Assert.Equal(prefix, row.CompanyName); Assert.Equal(prefix, row.PositionName); Assert.Equal(new DateOnly(1990, 2, 3), row.DateOfBirth);
        Assert.Equal(new[] { "Active", "Inactive" }, await Post<string[]>("filter-values", new TableValuesRequest { Query = query, Field = "isActive" }));
        Assert.Single(await Post<LookupOption[]>("company-options", new LookupRequest { Text = prefix }));
        Assert.Single(await Post<LookupOption[]>("position-options", new LookupRequest { Text = prefix }));
        var response = await client.PutAsJsonAsync("/api/Persons/" + bId, new UpdatePersonRequest { FirstName = prefix + " B", CompanyId = null, PositionId = null, IsActive = true });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); var cleared = (await response.Content.ReadFromJsonAsync<PersonDetailsDto>())!; Assert.Null(cleared.CompanyId); Assert.Null(cleared.PositionId);
        query.PageSize = 101; Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Persons/query", query)).StatusCode);
        async Task<T> Post<T>(string route, object body)
        { var result = await client.PostAsJsonAsync("/api/Persons/" + route, body); Assert.Equal(HttpStatusCode.OK, result.StatusCode); return (await result.Content.ReadFromJsonAsync<T>())!; }
    }
}
