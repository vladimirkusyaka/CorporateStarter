using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Components.Server.Circuits;
using CorporateStarter.Web.Components;
using CorporateStarter.Web.Services.Api;
using CorporateStarter.Web.Services.Auth;
using CorporateStarter.Client.Abstractions.Connection;
using CorporateStarter.Client.UI;
using CorporateStarter.Client.UI.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCorporateStarterBrowserClient(options =>
    builder.Configuration.GetSection(SessionUiOptions.SectionName).Bind(options));

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

builder.Services.AddScoped<CircuitConnectionMonitor>();
builder.Services.AddScoped<CircuitHandler>(services => services.GetRequiredService<CircuitConnectionMonitor>());
builder.Services.AddScoped<IClientConnectionState>(services => services.GetRequiredService<CircuitConnectionMonitor>());

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
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ClientUiAssembly).Assembly);

app.MapReverseProxy();

app.Run();
