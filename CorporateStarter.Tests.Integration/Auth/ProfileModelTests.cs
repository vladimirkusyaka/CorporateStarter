using CorporateStarter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace CorporateStarter.Tests.Integration.Auth;

public sealed class ProfileModelTests
{
    [Fact]
    public void Migration_matches_model_without_connecting_to_database()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=localhost;Database=unused").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Contains("20261012000000_AddProfileAppearance", db.Database.GetMigrations());
    }
}
