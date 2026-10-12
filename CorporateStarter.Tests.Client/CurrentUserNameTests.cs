using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Shared.Dtos.Auth;
using Xunit;
namespace CorporateStarter.Tests.Client;

public sealed class CurrentUserNameTests
{
    [Theory]
    [InlineData("  Alice Smith  ", "asmith", "Alice Smith", "A")]
    [InlineData(null, "admin", "admin", "A")]
    [InlineData("  ", " login ", "login", "L")]
    [InlineData("\u041E\u043B\u0435\u043D\u0430", "login", "\u041E\u043B\u0435\u043D\u0430", "\u041E")]
    [InlineData("e\u0301mile", "login", "e\u0301mile", "E\u0301")]
    public void Name_and_initial_use_the_same_display_name_or_login(string? display, string login, string expected, string initial)
    {
        var name = CurrentUserClient.Name(new UserProfileDto { DisplayName = display, Login = login });
        Assert.Equal(expected, name); Assert.Equal(initial, CurrentUserClient.Initial(name));
    }
}