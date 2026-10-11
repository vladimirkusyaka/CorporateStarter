using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Infrastructure.Persistence.Repositories.Audit;
using CorporateStarter.Infrastructure.Persistence.Repositories.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Tests.Integration.Auth;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;
namespace CorporateStarter.Tests.Integration.Audit;

public sealed class SecurityEventTableTests(CorporateStarterApiFactory factory) : IClassFixture<CorporateStarterApiFactory>
{
    [Fact]
    public async Task Table_preserves_anonymous_events_orders_filters_finds_and_loads_details()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var marker = Guid.NewGuid().ToString("N");
        var older = new SecurityEvent { EventType = "Login", Severity = "Info", Outcome = "Success", CorrelationId = marker, CreatedAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var newer = new SecurityEvent { EventType = "Login", Severity = "Warning", Outcome = "Failure", CorrelationId = marker, CreatedAtUtc = new(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), IpAddress = "127.0.0.1", DetailsJson = "{\"reason\":\"test\"}" };
        db.SecurityEvents.AddRange(older, newer); await db.SaveChangesAsync();
        var repository = new SecurityEventTableRepository(db);
        var query = new TableRequest { PageSize = 1, Filters = [new("correlationId", "eq", JsonSerializer.SerializeToElement(marker))] };
        var page = await repository.QueryAsync(query, default);
        Assert.Equal(2, page.TotalCount); Assert.Equal(newer.Id, Assert.Single(page.Items).Id); Assert.Null(page.Items[0].SubjectUserEmail);
        Assert.Equal(2, (await repository.FindAsync(new() { Query = query, LocateId = older.Id }, default)).PageNumber);
        Assert.Equal(newer.Id, (await repository.FindAsync(new() { Query = query, Text = "127.0.0.1" }, default)).Id);
        Assert.Equal(newer.DetailsJson, (await repository.GetDetailsAsync(newer.Id, default))!.DetailsJson);
        Assert.Null(await repository.GetDetailsAsync(Guid.NewGuid(), default));
        query.Filters.Add(new("severity", "eq", JsonSerializer.SerializeToElement("Warning")));
        Assert.Equal(newer.Id, Assert.Single((await repository.QueryAsync(query, default)).Items).Id);
        query.Filters.RemoveAt(1); query.Filters.Add(new("createdAtUtc", "contains", JsonSerializer.SerializeToElement("2026-01-01")));
        Assert.Equal(older.Id, Assert.Single((await repository.QueryAsync(query, default)).Items).Id);
        query.Filters.RemoveAt(1); query.Sorts.Add(new("createdAtUtc", "asc"));
        Assert.Equal(older.Id, Assert.Single((await repository.QueryAsync(query, default)).Items).Id);
        query.PageSize = 101;
        await Assert.ThrowsAsync<ArgumentException>(() => repository.QueryAsync(query, default));
    }
}
