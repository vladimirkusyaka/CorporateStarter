using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Browser.Api;
using CorporateStarter.Client.Browser.auth;
using CorporateStarter.Client.Browser.Auth;
using CorporateStarter.Client.Core;
using CorporateStarter.Client.UI.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MudBlazor.Services;

namespace CorporateStarter.Client.UI;

public static class ClientServiceCollectionExtensions
{
    public static IServiceCollection AddCorporateStarterBrowserClient(this IServiceCollection services,
        Action<SessionUiOptions>? configure = null)
    {
        services.AddMudServices();
        services.TryAddScoped<BrowserCapabilityProbe>();
        services.TryAddScoped<BrowserLoginTransport>();
        services.TryAddScoped<IClientLoginTransport>(sp => sp.GetRequiredService<BrowserLoginTransport>());
        services.TryAddScoped<IClientSessionTransport>(sp => sp.GetRequiredService<BrowserLoginTransport>());
        services.TryAddScoped<IClientLogoutTransport>(sp => sp.GetRequiredService<BrowserLoginTransport>());
        services.AddCorporateStarterClientCore();
        services.TryAddScoped<IClientApiTransport, BrowserApiTransport>();
        services.TryAddTransient<BrowserIdleMonitor>();
        services.TryAddTransient<BrowserSessionMonitor>();
        var options = services.AddOptions<SessionUiOptions>();
        if (configure is not null) options.Configure(configure);
        options.Validate(x => x.CheckIndicatorDelaySeconds is >= 0 and <= 30,
            "SessionUi:CheckIndicatorDelaySeconds must be between 0 and 30.").ValidateOnStart();
        return services;
    }
}
