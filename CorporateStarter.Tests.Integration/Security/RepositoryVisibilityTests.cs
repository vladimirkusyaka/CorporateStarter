using System.Data.Common;
using System.Reflection;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Positions;
using CorporateStarter.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
namespace CorporateStarter.Tests.Integration.Security;

// Compile the real repository queries, stopping before any database IO.
public sealed class RepositoryVisibilityTests
{
    public static IEnumerable<object[]> Cases()
    {
        var repositories = new[] { "MasterData.Countries.Country", "MasterData.Cities.City", "MasterData.Positions.Position",
            "Directory.Companies.Company", "Directory.Persons.Person", "Security.User", "Security.Role", "Security.Permission" };
        foreach (var name in repositories)
            foreach (var view in new[] { false, true }) yield return [name, view];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Lists_filter_in_SQL_and_not_after_paging(string name, bool view)
    {
        await using var db = Context();
        var type = typeof(PositionReadRepository).Assembly.GetType("CorporateStarter.Infrastructure.Persistence.Repositories." + name + "ReadRepository")!;
        var repo = Activator.CreateInstance(type, db, new Access(view))!;
        var error = await Assert.ThrowsAsync<CapturedSql>(() => (Task)type.GetMethod("GetListAsync")!.Invoke(repo, [CancellationToken.None])!);
        var rootAlias = System.Text.RegularExpressions.Regex.Match(error.Message, "\\nFROM \"[^\"]+\" AS (\\w+)").Groups[1].Value;
        Assert.NotEmpty(rootAlias);
        var hasActivePredicate = error.Message.Contains("WHERE " + rootAlias + ".\"IsActive\"");
        Assert.Equal(!view, hasActivePredicate);
    }
    [Fact]
    public async Task Uniqueness_includes_hidden_positions()
    {
        await using var db = Context();
        var repo = new PositionReadRepository(db, new Access(false));
        var error = await Assert.ThrowsAsync<CapturedSql>(() => repo.ExistsByNameAsync("Archived", null, default));
        Assert.DoesNotContain("IsActive", error.Message);
    }
    [Theory]
    [InlineData("MasterData.Cities.City", "SearchCountriesAsync")]
    [InlineData("Directory.Companies.Company", "SearchCitiesAsync")]
    [InlineData("Directory.Persons.Person", "SearchCompaniesAsync")]
    [InlineData("Directory.Persons.Person", "SearchPositionsAsync")]
    public async Task Lookup_queries_also_filter_hidden_records(string name, string method)
    {
        await using var db = Context();
        var type = typeof(PositionReadRepository).Assembly.GetType("CorporateStarter.Infrastructure.Persistence.Repositories." + name + "TableRepository")!;
        var repo = Activator.CreateInstance(type, db, new Access(false))!;
        var error = await Assert.ThrowsAsync<CapturedSql>(() => (Task)type.GetMethod(method)!.Invoke(repo, [new LookupRequest(), CancellationToken.None])!);
        Assert.Matches("WHERE[^;]*IsActive", error.Message);
    }
    private static AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=unused")
        .AddInterceptors(new NoConnection(), new CaptureCommand()).Options);
    private sealed class Access(bool view) : IEntityAccess
    { public TableCapabilities For(string resource) => new(false, false, false, view, false); }
    private sealed class CapturedSql(string sql) : Exception(sql);
    private sealed class NoConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }
    private sealed class CaptureCommand : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) =>
            throw new CapturedSql(command.CommandText);
    }
}
