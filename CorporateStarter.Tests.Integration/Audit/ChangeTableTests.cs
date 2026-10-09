using CorporateStarter.Core.Entities.Audit;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Infrastructure.Persistence.Repositories.Audit;
using CorporateStarter.Shared.Common;
using CorporateStarter.Tests.Integration.Auth;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;
namespace CorporateStarter.Tests.Integration.Audit;

public sealed class ChangeTableTests(CorporateStarterApiFactory factory) : IClassFixture<CorporateStarterApiFactory>
{
    [Fact]
    public async Task Query_find_and_details_share_scope_and_newest_first_order()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var marker = Guid.NewGuid().ToString("N");
        var older = new AuditLog { EntityName = "Person", EntityId = marker, Action = "Create", CreatedAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), NewValuesJson = "{}" };
        var newer = new AuditLog { EntityName = "Person", EntityId = marker, Action = "Update", CreatedAtUtc = new(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), OldValuesJson = "{}", NewValuesJson = "{}" };
        var hidden = new AuditLog { EntityName = "AuthSession", EntityId = marker, Action = "Create" };
        db.AuditLogs.AddRange(older, newer, hidden); await db.SaveChangesAsync();
        var repository = new ChangeTableRepository(db);
        var query = new TableRequest { PageSize = 1, Filters = [new("entityId", "eq", JsonSerializer.SerializeToElement(marker))] };
        var page = await repository.QueryAsync(query, default);
        Assert.Equal(2, page.TotalCount); Assert.Equal(newer.Id, Assert.Single(page.Items).Id);
        Assert.Equal(2, (await repository.FindAsync(new() { Query = query, LocateId = older.Id }, default)).PageNumber);
        Assert.Null((await repository.FindAsync(new() { Query = query, LocateId = hidden.Id }, default)).Id);
        Assert.Null(await repository.GetDetailsAsync(hidden.Id, default));
        Assert.Equal("{}", (await repository.GetDetailsAsync(newer.Id, default))!.OldValuesJson);
        query.Filters.Add(new("createdAtUtc", "contains", JsonSerializer.SerializeToElement("2026-01-01")));
        Assert.Equal(older.Id, Assert.Single((await repository.QueryAsync(query, default)).Items).Id);
        query.Sorts.Add(new("createdAtUtc", "asc"));
        Assert.Equal(older.Id, Assert.Single((await repository.QueryAsync(query, default)).Items).Id);
    }
}
