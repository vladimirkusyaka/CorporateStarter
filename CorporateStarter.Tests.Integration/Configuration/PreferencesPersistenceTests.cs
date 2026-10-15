using CorporateStarter.Application.Common.Interfaces;
using CorporateStarter.Application.Common.Interfaces.Repositories.Preferences;
using CorporateStarter.Tests.Integration.Auth;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace CorporateStarter.Tests.Integration.Configuration;

public sealed class PreferencesPersistenceTests(CorporateStarterApiFactory factory) : IClassFixture<CorporateStarterApiFactory>
{
    [Fact]
    public async Task Shared_name_persists_and_stale_revision_cannot_overwrite_it()
    {
        using var first = factory.Services.CreateScope(); using var second = factory.Services.CreateScope();
        var a = first.ServiceProvider.GetRequiredService<IPreferencesRepository>();
        var b = second.ServiceProvider.GetRequiredService<IPreferencesRepository>();
        var before = await a.GetAsync(default);
        var saved = await a.UpdateAsync(new() { Name = "Example project", Revision = before.Revision }, default);
        Assert.NotNull(saved);
        Assert.Null(await b.UpdateAsync(new() { Name = "Stale", Revision = before.Revision }, default));
        Assert.Equal("Example project", (await b.GetAsync(default)).Name);
    }
}
