using CorporateStarter.Client.Core.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
namespace CorporateStarter.Client.UI.Components.Layout;

public partial class MainLayout
{
    [Inject] private ProfilePreferences Preferences { get; set; } = default!;
    [Inject] private IJSRuntime AppearanceJs { get; set; } = default!;
    private MudThemeProvider? _themeProvider;
    private bool _systemDark;
    private bool DarkAppearance => Preferences.Theme == "Dark" || Preferences.Theme == "System" && _systemDark;
    private void AppearanceChanged() => _ = InvokeAsync(() => { if (!_disposed) StateHasChanged(); });
    private async Task InitializeAppearanceAsync()
    {
        if (_themeProvider is not null)
        {
            _systemDark = await _themeProvider.GetSystemDarkModeAsync();
            await _themeProvider.WatchSystemDarkModeAsync(dark => InvokeAsync(() => { _systemDark = dark; if (!_disposed) StateHasChanged(); }));
        }
        try
        {
            await using var module = await AppearanceJs.InvokeAsync<IJSObjectReference>("import", "./_content/CorporateStarter.Client.UI/js/profile.js?v=1");
            Preferences.SetDeviceZone(await module.InvokeAsync<string>("deviceTimeZone"));
        }
        catch (JSException) { /* UTC remains the explicit fallback. */ }
    }
}
