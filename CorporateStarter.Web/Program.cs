using Microsoft.AspNetCore.Diagnostics;
using CorporateStarter.Web.Components;
using CorporateStarter.Web.Services.Api;
using CorporateStarter.Client.Browser.auth;
using CorporateStarter.Client.Browser.Auth;
using MudBlazor.Services;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Web.Services.Auth;
using Microsoft.AspNetCore.Components.Server.Circuits;
using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Browser.Api;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.MasterData.Countries;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddScoped<BrowserCapabilityProbe>();
builder.Services.AddScoped<BrowserLoginTransport>();
builder.Services.AddTransient<BrowserIdleMonitor>();

builder.Services.AddScoped<IClientLoginTransport>(services =>
    services.GetRequiredService<BrowserLoginTransport>());

builder.Services.AddScoped<IClientSessionTransport>(services =>
    services.GetRequiredService<BrowserLoginTransport>());

builder.Services.AddScoped<ClientAuthStateStore>();

builder.Services.AddScoped<IClientAuthState>(services =>
    services.GetRequiredService<ClientAuthStateStore>());

builder.Services.AddScoped<ClientSessionCoordinator>();
builder.Services.AddScoped<IClientApiTransport, BrowserApiTransport>();
builder.Services.AddScoped<ClientApiClient>();
builder.Services.AddScoped<CountriesClient>();

builder.Services.Configure<ApiOptions>(
    builder.Configuration.GetSection("Api"));

builder.Services.AddHttpClient(
    "CorporateStarterApi",
    (serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiOptions>>()
            .Value;

        client.BaseAddress = new Uri(options.BaseUrl);
    });

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddScoped<IClientLogoutTransport>(services =>
    services.GetRequiredService<BrowserLoginTransport>());
builder.Services.AddTransient<BrowserSessionMonitor>();

builder.Services.AddScoped<CircuitConnectionMonitor>();

builder.Services.AddScoped<CircuitHandler>(services =>
    services.GetRequiredService<CircuitConnectionMonitor>());

builder.Services.AddOptions<SessionUiOptions>()
    .Bind(builder.Configuration.GetSection(SessionUiOptions.SectionName))
    .Validate(options => options.CheckIndicatorDelaySeconds is >= 0 and <= 30,
        "SessionUi:CheckIndicatorDelaySeconds must be between 0 and 30.")
    .ValidateOnStart();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var feature = context.Features.Get<IStatusCodePagesFeature>();

        if (feature is not null)
            feature.Enabled = false;
    }

    await next(context);
});
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapReverseProxy();

app.Run();
