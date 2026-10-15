using System.Text.Json;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Infrastructure.Persistence.Repositories.Preferences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
namespace CorporateStarter.Tests.Integration.Configuration;

public sealed class PreferencesStoreTests
{
    [Fact]
    public void Connection_projection_excludes_credentials_and_migration_matches_the_model()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=db.internal;Port=5433;Database=application;Username=private-user;Password=private-password;SSL Mode=Require;Application Name=private-app").Options);
        var dto = new PreferencesRepository(db).GetConnections();
        Assert.Equal("db.internal", dto.Server); Assert.Equal(5433, dto.Port); Assert.Equal("application", dto.Database);
        var json = JsonSerializer.Serialize(dto);
        Assert.DoesNotContain("private", json); Assert.DoesNotContain("SSL", json);
        Assert.False(db.Database.HasPendingModelChanges());
        var sql = db.GetService<IMigrator>().GenerateScript("20261012000000_AddProfileAppearance", "20261014000000_AddApplicationPreferences");
        Assert.Contains("CREATE TABLE", sql);
        Assert.Contains("CorporateStarter", sql);
    }
}
