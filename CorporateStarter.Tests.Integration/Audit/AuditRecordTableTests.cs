using CorporateStarter.Core.Entities.Audit;
using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Infrastructure.Persistence.Repositories.Audit;
using CorporateStarter.Shared.Common;
using CorporateStarter.Tests.Integration.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;
namespace CorporateStarter.Tests.Integration.Audit;

public sealed class AuditRecordTableTests(CorporateStarterApiFactory factory) : IClassFixture<CorporateStarterApiFactory>
{
    [Fact]
    public async Task Migration_backfills_only_historical_snapshots_and_tolerates_invalid_json()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var migrator = db.GetService<IMigrator>();
        var good = Guid.NewGuid(); var deleted = Guid.NewGuid(); var invalid = Guid.NewGuid();
        await migrator.MigrateAsync("20261014000000_AddApplicationPreferences");
        try
        {
            var now = DateTime.UtcNow;
            var personJson = "{\"FirstName\":\"Jane\",\"LastName\":\"Doe\"}";
            var cityJson = "{\"Name\":\"Old city\"}";
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AuditLogs" ("Id", "EntityName", "EntityId", "Action", "CreatedAtUtc", "NewValuesJson")
                VALUES ({good}, 'Person', 'legacy-good', 'Create', {now}, {personJson})
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AuditLogs" ("Id", "EntityName", "EntityId", "Action", "CreatedAtUtc", "OldValuesJson")
                VALUES ({deleted}, 'City', 'legacy-deleted', 'Delete', {now}, {cityJson})
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AuditLogs" ("Id", "EntityName", "EntityId", "Action", "CreatedAtUtc", "NewValuesJson")
                VALUES ({invalid}, 'City', 'legacy-invalid', 'Create', {now}, 'broken-json')
                """);
        }
        finally { await migrator.MigrateAsync(); }
        Assert.Equal("Jane Doe", (await db.AuditLogs.SingleAsync(x => x.Id == good)).EntityDisplayName);
        Assert.Equal("Old city", (await db.AuditLogs.SingleAsync(x => x.Id == deleted)).EntityDisplayName);
        Assert.Null((await db.AuditLogs.SingleAsync(x => x.Id == invalid)).EntityDisplayName);
        Assert.Null((await db.AuditLogs.SingleAsync(x => x.Id == good)).CorrelationId);
    }

    [Fact]
    public async Task Labels_ids_and_correlation_are_searchable_and_returned_in_details()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var marker = Guid.NewGuid().ToString("N");
        var change = new AuditLog { EntityName = "City", EntityId = "Id:" + Guid.NewGuid(), EntityDisplayName = "Historical " + marker, CorrelationId = marker, Action = "Update" };
        db.AuditLogs.Add(change);
        var user = new User { Login = marker, Email = marker + "@example.test", DisplayName = "Historical user " + marker, PasswordHash = "unused" };
        db.Users.Add(user);
        var item = new SecurityEvent { SubjectUserId = user.Id, SubjectUserEmail = user.Email, EventType = "Test", CorrelationId = marker };
        db.SecurityEvents.Add(item); await db.SaveChangesAsync();
        db.ChangeTracker.Clear(); // Exercise lookup of an untracked subject on the next save.
        var next = new SecurityEvent { SubjectUserId = user.Id, EventType = "TestLookup" };
        db.SecurityEvents.Add(next); await db.SaveChangesAsync();
        Assert.Equal(user.DisplayName, next.SubjectUserDisplayName);
        var changes = new ChangeTableRepository(db);
        var query = new TableRequest { Filters = [new("correlationId", "eq", JsonSerializer.SerializeToElement(marker))] };
        Assert.Equal(change.Id, Assert.Single((await changes.QueryAsync(query, default)).Items).Id);
        Assert.Equal(change.Id, (await changes.FindAsync(new() { Query = query, Text = "Historical" }, default)).Id);
        query.Filters.Add(new("id", "eq", JsonSerializer.SerializeToElement(change.Id.ToString())));
        query.Filters.Add(new("entityDisplayName", "contains", JsonSerializer.SerializeToElement(marker)));
        Assert.Single((await changes.QueryAsync(query, default)).Items);
        Assert.Equal(change.EntityDisplayName, (await changes.GetDetailsAsync(change.Id, default))!.EntityDisplayName);
        var events = new SecurityEventTableRepository(db);
        var eq = new TableRequest { Filters = [new("correlationId", "eq", JsonSerializer.SerializeToElement(marker)), new("subjectUserId", "eq", JsonSerializer.SerializeToElement(user.Id.ToString()))] };
        Assert.Equal(item.Id, Assert.Single((await events.QueryAsync(eq, default)).Items).Id);
        Assert.Equal(item.Id, (await events.FindAsync(new() { Query = eq, Text = "Historical user" }, default)).Id);
        Assert.Equal(user.DisplayName, (await events.GetDetailsAsync(item.Id, default))!.SubjectUserDisplayName);
        // Renaming/deleting the source must never alter a historical event's title.
        var loaded = await db.Users.SingleAsync(x => x.Id == user.Id);
        loaded.DisplayName = "Renamed"; await db.SaveChangesAsync();
        db.Users.Remove(loaded); await db.SaveChangesAsync();
        Assert.Equal(user.DisplayName, (await events.GetDetailsAsync(item.Id, default))!.SubjectUserDisplayName);
    }
}
