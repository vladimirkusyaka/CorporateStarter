using CorporateStarter.Client.Abstractions.Connection;
using CorporateStarter.Client.UI;
using CorporateStarter.Client.UI.Configuration;
using CorporateStarter.Standalone;
using CorporateStarter.Standalone.Connection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddCorporateStarterBrowserClient(options =>
    builder.Configuration.GetSection(SessionUiOptions.SectionName).Bind(options));
builder.Services.AddSingleton<IClientConnectionState, StandaloneConnectionState>();

await builder.Build().RunAsync();
