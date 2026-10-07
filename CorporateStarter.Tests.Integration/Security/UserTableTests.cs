using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.Users;
using CorporateStarter.Tests.Integration.Auth;
using CorporateStarter.Tests.Integration.MasterData;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
namespace CorporateStarter.Tests.Integration.Security;

// Requires Docker/PostgreSQL, unlike UserApiContractTests.
public sealed class UserTableTests(CorporateStarterApiFactory factory) : IClassFixture<CorporateStarterApiFactory>
{
    [Fact]
    public async Task User_table_queries_find_filters_and_preserves_inactive_roles_on_update()
    {
        using var app = factory.WithWebHostBuilder(builder => builder
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureTestServices(services => services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "UserTableTest";
                options.DefaultChallengeScheme = "UserTableTest";
                options.DefaultForbidScheme = "UserTableTest";
            }).AddScheme<AuthenticationSchemeOptions, TableTestAuthentication>("UserTableTest", _ => { })));
        using var client = app.CreateClient();
        var prefix = "usr" + Guid.NewGuid().ToString("N");
        Guid first, second, inactiveRole;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            client.DefaultRequestHeaders.Add("X-Test-User", (await db.Users.Select(x => x.Id).FirstAsync()).ToString());
            var role = new Role { Id = Guid.NewGuid(), Name = prefix, IsActive = false };
            var activeRole = await db.Roles.FirstAsync(x => x.IsActive);
            var a = new User { Id = Guid.NewGuid(), Login = prefix + "a", Email = prefix + "a@example.test", DisplayName = "User Alpha", IsActive = true, PasswordHash = "test-only" };
            var b = new User { Id = Guid.NewGuid(), Login = prefix + "b", Email = prefix + "b@example.test", DisplayName = "User Beta", IsActive = false, PasswordHash = "test-only" };
            db.Roles.Add(role); db.Users.AddRange(a, b);
            db.UserRoles.AddRange(new UserRole { UserId = a.Id, RoleId = role.Id }, new UserRole { UserId = a.Id, RoleId = activeRole.Id });
            await db.SaveChangesAsync(); first = a.Id; second = b.Id; inactiveRole = role.Id;
        }
        client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(',', AppPermissions.UsersRead, AppPermissions.UsersUpdate, AppPermissions.UsersDelete));
        var query = new TableRequest { PageSize = 1, Filters = [new("login", "contains", JsonSerializer.SerializeToElement(prefix))] };
        var page = await Post<PagedResult<UserTableItemDto>>("query", query);
        Assert.Equal(2, page.TotalCount); Assert.Equal(first, Assert.Single(page.Items).Id);
        Assert.Equal(2, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, LocateId = second })).PageNumber);
        Assert.Equal(second, (await Post<TableFindResult>("find", new TableFindRequest { Query = query, Text = "User Beta" })).Id);
        Assert.Equal(new[] { "Active", "Inactive" }, await Post<string[]>("filter-values", new TableValuesRequest { Query = query, Field = "isActive" }));
        query.Filters.Add(new("isActive", "in", Values: [JsonSerializer.SerializeToElement(false)]));
        Assert.Equal(second, Assert.Single((await Post<PagedResult<UserTableItemDto>>("query", query)).Items).Id);
        query.PageSize = 101;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Users/query", query)).StatusCode);
        var details = (await client.GetFromJsonAsync<UserDetailsDto>("/api/Users/" + first))!;
        Assert.Contains(details.Roles, x => x.RoleId == inactiveRole);
        var response = await client.PutAsJsonAsync("/api/Users/" + first, new UpdateUserRequest { Login = details.Login, Email = details.Email, DisplayName = "Changed", IsActive = true });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.True(await db.UserRoles.AnyAsync(x => x.UserId == first && x.RoleId == inactiveRole));
        }
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("/api/Users/" + second)).StatusCode);
        async Task<T> Post<T>(string route, object body)
        {
            var result = await client.PostAsJsonAsync("/api/Users/" + route, body);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            return (await result.Content.ReadFromJsonAsync<T>())!;
        }
    }
}
