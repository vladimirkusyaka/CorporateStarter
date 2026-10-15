using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Core.Configuration;
using CorporateStarter.Shared.Dtos.Configuration;
using Microsoft.AspNetCore.Components;
using System.Globalization;
namespace CorporateStarter.Client.UI.Components.Pages.Preferences;

public partial class Preferences : IDisposable
{
    [Inject] private PreferencesClient Client { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CancellationTokenSource _lifetime = new();
    private PreferencesDto? _info;
    private ConnectionInfoDto? _connections;
    private string _name = "";
    private string? _error, _connectionsError;
    private bool _busy, _saved, _disposed, _connectionsBusy;
    private int _tab;
    private static string ClientMode => OperatingSystem.IsBrowser() ? "Blazor WebAssembly Standalone" : "Blazor Server-side";
    private string BrowserApiEndpoint => new Uri(new Uri(Navigation.BaseUri), "/api/").AbsoluteUri;
    private static string BuildDate(DateTimeOffset? value) => value?.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "Not recorded";
    protected override Task OnAfterRenderAsync(bool firstRender) => firstRender ? LoadAsync() : Task.CompletedTask;

    private async Task LoadAsync()
    {
        if (_busy) return;
        _busy = true; _error = null; _saved = false;
        _connections = null; _connectionsError = null;
        try
        {
            var info = await Client.GetAsync(_lifetime.Token);
            if (_disposed) return;
            _info = info; _name = info.Name;
        }
        catch (OperationCanceledException) when (_disposed) { }
        catch (Exception) { _error = "Could not load preferences. Please retry."; }
        finally { _busy = false; if (!_disposed) StateHasChanged(); }
    }
    private async Task SaveAsync()
    {
        if (_busy || _info is not { CanUpdate: true }) return;
        _error = null; _saved = false;
        if (string.IsNullOrWhiteSpace(_name) || _name.Length > 100 || _name.Any(char.IsControl))
        { _error = "Enter an application name (1-100 characters, without control characters)."; return; }
        _busy = true;
        try
        {
            var result = await Client.UpdateAsync(new() { Name = _name.Trim(), Revision = _info.Revision }, _lifetime.Token);
            if (_disposed) return;
            _info.Name = result.Name; _info.Revision = result.Revision; _name = result.Name; _saved = true;
        }
        catch (OperationCanceledException) when (_disposed) { }
        catch (ClientApiHttpException ex) when (ex.StatusCode == 409)
        { _error = "Another administrator changed the name. Reload saved values before editing again."; }
        catch (ClientApiHttpException ex) when (ex.StatusCode == 403)
        { if (_info is not null) _info.CanUpdate = false; _error = "You no longer have permission to update preferences."; }
        catch (Exception)
        { _error = "Saving was not confirmed. Reload saved values before trying again."; }
        finally { _busy = false; }
    }
    private async Task ChangeTabAsync(int tab)
    {
        _tab = tab; _saved = false;
        if (tab == 2 && _connections is null) await LoadConnectionsAsync();
    }
    private async Task LoadConnectionsAsync()
    {
        if (_connectionsBusy || _info is not { CanViewConnections: true }) return;
        _connectionsBusy = true; _connectionsError = null; _connections = null;
        try
        {
            var result = await Client.GetConnectionsAsync(_lifetime.Token);
            if (!_disposed) _connections = result;
        }
        catch (OperationCanceledException) when (_disposed) { }
        catch (ClientApiHttpException ex) when (ex.StatusCode == 403)
        { _connectionsError = "You do not have permission to view connections."; }
        catch (Exception) { _connectionsError = "Could not load connection information. Please retry."; }
        finally { _connectionsBusy = false; }
    }
    public void Dispose()
    {
        _disposed = true; _connections = null; _info = null;
        _lifetime.Cancel(); _lifetime.Dispose();
    }
}
