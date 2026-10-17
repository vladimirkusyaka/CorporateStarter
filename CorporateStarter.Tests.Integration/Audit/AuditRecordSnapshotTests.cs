using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Core.Entities.Audit;
using CorporateStarter.Core.Entities.Directory;
using CorporateStarter.Core.Entities.MasterData;
using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace CorporateStarter.Tests.Integration.Audit;

// Exercises the actual SaveChanges audit pipeline without a database connection.
// SQL persistence and filtering are covered separately by AuditRecordTableTests.
public sealed class AuditRecordSnapshotTests
{
    private static AppDbContext Create() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
        .AddInterceptors(new SuppressSave()).Options, correlationIdProvider: new Correlation());

    [Fact]
    public async Task Name_is_captured_on_create_rename_and_delete_and_old_label_is_immutable()
    {
        await using var db = Create();
        var country = new Country { Name = "Original" };
        db.Countries.Add(country); await db.SaveChangesAsync();
        var created = Assert.Single(db.AuditLogs.Local);
        Assert.Equal("Original", created.EntityDisplayName);
        Assert.Equal("batch-test", created.CorrelationId);
        Assert.Contains(country.Id.ToString(), created.EntityId);
        db.ChangeTracker.AcceptAllChanges();
        country.Name = "Renamed"; await db.SaveChangesAsync();
        var updated = Assert.Single(db.AuditLogs.Local, x => x.Action == "Update");
        Assert.Equal("Renamed", updated.EntityDisplayName);
        Assert.Equal("Original", created.EntityDisplayName);
        db.ChangeTracker.AcceptAllChanges();
        db.Countries.Remove(country); await db.SaveChangesAsync();
        Assert.Equal("Renamed", Assert.Single(db.AuditLogs.Local, x => x.Action == "Delete").EntityDisplayName);
    }

    [Theory]
    [InlineData("Country", "Capital")]
    [InlineData("City", "Capital")]
    [InlineData("Company", "Capital")]
    [InlineData("Position", "Capital")]
    [InlineData("Role", "Capital")]
    [InlineData("Permission", "Capital")]
    [InlineData("ApplicationPreferences", "Capital")]
    [InlineData("Person", "Jane Doe")]
    [InlineData("User", "Display")]
    [InlineData("UnknownEntity", null)]
    public void Formatter_uses_only_known_identity_fields(string entity, string? expected)
    {
        Assert.Equal(expected, AuditRecordName.FromValues(entity, new Dictionary<string, object?>
        { ["Name"] = " Capital ", ["FirstName"] = " Jane ", ["LastName"] = "Doe", ["DisplayName"] = "Display", ["Login"] = "login" }));
    }

    [Fact]
    public void Blank_names_and_partial_names_have_explicit_fallbacks()
    {
        Assert.Equal("login", AuditRecordName.FromValues("User", new Dictionary<string, object?> { ["DisplayName"] = " ", ["Login"] = "login" }));
        Assert.Equal("Jane", AuditRecordName.FromValues("Person", new Dictionary<string, object?> { ["FirstName"] = "Jane", ["LastName"] = null }));
        Assert.Null(AuditRecordName.FromValues("Country", new Dictionary<string, object?>()));
    }

    [Fact]
    public async Task Security_events_capture_user_name_and_keep_anonymous_and_original_event_context()
    {
        await using var db = Create();
        var user = new User { Login = "jane", DisplayName = "Jane Doe", Email = "jane@example.test" };
        db.Users.Add(user);
        var item = new SecurityEvent { SubjectUserId = user.Id, SubjectUserEmail = user.Email, EventType = "LoginSucceeded", CorrelationId = "explicit" };
        var anonymous = new SecurityEvent { EventType = "LoginFailed" };
        db.SecurityEvents.AddRange(item, anonymous); await db.SaveChangesAsync();
        Assert.Equal("Jane Doe", item.SubjectUserDisplayName);
        Assert.Equal("explicit", item.CorrelationId);
        Assert.Null(anonymous.SubjectUserDisplayName);
        Assert.Equal("batch-test", anonymous.CorrelationId);
        db.ChangeTracker.AcceptAllChanges();
        user.DisplayName = "Changed"; await db.SaveChangesAsync();
        Assert.Equal("Jane Doe", item.SubjectUserDisplayName);
    }

    [Fact]
    public void Migration_snapshot_matches_model_and_upgrade_script_contains_backfill()
    {
        using var db = Create();
        Assert.False(db.Database.HasPendingModelChanges());
        var sql = db.GetService<IMigrator>().GenerateScript("20261014000000_AddApplicationPreferences", "20261016000000_AddAuditRecordNames");
        Assert.Contains("audit_record_name", sql);
        Assert.Contains("data_exception", sql);
        Assert.Contains("IX_AuditLogs_CorrelationId", sql);
    }

    private sealed class Correlation : ICorrelationIdProvider { public string CorrelationId => "batch-test"; }
    private sealed class SuppressSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));
    }
}
