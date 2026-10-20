using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Infrastructure.Persistence.Tables;
using CorporateStarter.Shared.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Xunit;
namespace CorporateStarter.Tests.Integration.Configuration;

public sealed class LocalizationContractTests
{
    [Fact]
    public void Migration_snapshot_includes_nullable_profile_language()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var property = db.Model.FindEntityType(typeof(CorporateStarter.Core.Entities.Security.User))!.FindProperty("PreferredLanguage")!;
        Assert.True(property.IsNullable); Assert.Equal(5, property.GetMaxLength());
    }
    [Theory]
    [InlineData("de-DE", "DD.MM.YYYY", "Großbuchstabe")]
    [InlineData("ru-RU", "DD.MM.YYYY", "Заглавная буква")]
    [InlineData("en-US", "FMMM/FMDD/YYYY", "Uppercase letter")]
    public void Request_culture_formats_date_search_and_server_policy_messages(string language, string pattern, string label)
    {
        var old = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
            Assert.Contains(pattern, SqlDateText.Utc("\"CreatedAtUtc\""));
            Assert.Equal(label, UiText.Get("Uppercase letter", CultureInfo.CurrentUICulture));
        }
        finally { CultureInfo.CurrentUICulture = old; }
    }
}
