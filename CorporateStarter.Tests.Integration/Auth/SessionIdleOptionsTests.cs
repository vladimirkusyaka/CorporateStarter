using CorporateStarter.Application.Common.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CorporateStarter.Tests.Integration.Auth;

public sealed class SessionIdleOptionsTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(1440)]
    public void Configuration_ControlsExpirationBoundary(int minutes)
    {
        using var provider = Build(minutes);
        provider.GetRequiredService<IStartupValidator>().Validate();
        var policy = provider.GetRequiredService<SessionIdlePolicy>();
        var now = DateTime.UtcNow;
        Assert.Equal(TimeSpan.FromMinutes(minutes), policy.Timeout);
        Assert.True(policy.IsExpired(now.AddMinutes(-minutes), now));
        Assert.False(policy.IsExpired(now.AddMinutes(-minutes).AddTicks(1), now));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1441)]
    public void InvalidConfiguration_FailsStartup(int minutes)
    {
        using var provider = Build(minutes);
        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IStartupValidator>().Validate());
    }

    [Fact]
    public void MissingSetting_DefaultsToThirtyMinutes()
    {
        using var provider = Build(null);
        provider.GetRequiredService<IStartupValidator>().Validate();
        Assert.Equal(TimeSpan.FromMinutes(30),
            provider.GetRequiredService<SessionIdlePolicy>().Timeout);
    }

    private static ServiceProvider Build(int? minutes)
    {
        var settings = new Dictionary<string, string?>();
        if (minutes.HasValue) settings["SessionIdle:TimeoutMinutes"] = minutes.Value.ToString();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddOptions<SessionIdleOptions>()
            .Bind(configuration.GetSection(SessionIdleOptions.SectionName))
            .Validate(options => options.IsValid(),
                "SessionIdle:TimeoutMinutes must be between 1 and 1440.")
            .ValidateOnStart();
        services.AddSingleton(sp => new SessionIdlePolicy(
            sp.GetRequiredService<IOptions<SessionIdleOptions>>().Value));
        return services.BuildServiceProvider();
    }
}
