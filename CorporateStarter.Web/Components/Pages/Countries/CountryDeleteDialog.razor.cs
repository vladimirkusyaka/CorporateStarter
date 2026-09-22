using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.MasterData.Countries;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CorporateStarter.Web.Components.Pages.Countries;

public partial class CountryDeleteDialog : IDisposable
{
    [Parameter] public IReadOnlyList<CountryListItemDto> Countries { get; set; } = [];
    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;
    [Inject] private CountriesClient Client { get; set; } = default!;
    [Inject] private IClientAuthState Auth { get; set; } = default!;
    [Inject] private ILogger<CountryDeleteDialog> Logger { get; set; } = default!;
    private readonly CancellationTokenSource _lifetime = new();
    private (Guid Id, string Name)[] _items = [];
    private ClientAuthSnapshot _initial = default!;
    private bool _busy, _attempted, _disposed, _closed;
    private int _completed;
    private string? _error;
    private bool SameSession => Auth.Current.UserId == _initial.UserId &&
        Auth.Current.SessionGeneration == _initial.SessionGeneration &&
        Auth.Current.Status is ClientAuthStatus.Authenticated or ClientAuthStatus.Revalidating;
    private bool CanDelete => !_busy && !_attempted && !_disposed && !_closed &&
        _items.Length > 0 && SameSession && Auth.Current.Status == ClientAuthStatus.Authenticated;
    private string Confirmation => _items.Length == 1
        ? $"Do you really want to delete Country: {_items[0].Name}?"
        : $"Do you really want to delete {_items.Length} Countries?";

    protected override void OnInitialized()
    {
        _initial = Auth.Current;
        // Freeze IDs and names: the confirmation and the requests use the same selection.
        _items = Countries.DistinctBy(x => x.Id).Select(x => (x.Id, x.Name)).ToArray();
        Auth.StateChanged += OnSessionChanged;
        if (!CanDelete) ForceClose();
    }

    private void OnSessionChanged(object? sender, ClientAuthStateChangedEventArgs args)
    {
        _ = InvokeAsync(() =>
        {
            if (_disposed || _closed) return;
            if (Auth.Current.UserId != _initial.UserId ||
                Auth.Current.SessionGeneration != _initial.SessionGeneration ||
                Auth.Current.Status is ClientAuthStatus.Anonymous or ClientAuthStatus.UnsupportedEnvironment)
                ForceClose();
            else StateHasChanged();
        });
    }

    private async Task DeleteAsync()
    {
        if (!CanDelete) return;
        _busy = _attempted = true;
        try
        {
            foreach (var item in _items)
            {
                _lifetime.Token.ThrowIfCancellationRequested();
                if (!SameSession) throw new ClientApiTransportException("The session changed during deletion.");
                await Client.DeleteAsync(item.Id, _lifetime.Token);
                if (_closed || _disposed) return;
                _completed++;
                StateHasChanged();
            }
            _closed = true;
            Dialog.Close(DialogResult.Ok(_completed));
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Country deletion stopped after {Completed} of {Total} confirmed requests.",
                _completed, _items.Length);
            if (_closed || _disposed) return;
            var reason = ex is ClientApiHttpException http ? http.StatusCode switch
            {
                401 => "Sign in again before continuing.",
                403 => "You do not have permission to delete countries.",
                404 => "The next country was not found.",
                429 => "Too many requests. Please wait before trying again.",
                >= 400 and < 500 => "The server rejected the next deletion.",
                _ => "The result of the last request is unknown."
            } : "The result of the last request is unknown.";
            _error = $"Deletion stopped. {_completed} of {_items.Length} deletions confirmed. {reason} " +
                "Close this dialog to refresh the list before selecting countries again.";
        }
        finally { _busy = false; }
    }

    private void Close()
    {
        if (_busy || _closed || _disposed) return;
        _closed = true;
        if (_attempted) Dialog.Close(DialogResult.Ok<object?>(null)); // Refresh even if the response was lost.
        else Dialog.Cancel();
    }

    private void ForceClose()
    {
        if (_closed || _disposed) return;
        _closed = true;
        _lifetime.Cancel();
        Dialog.Cancel();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Auth.StateChanged -= OnSessionChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _items = [];
        GC.SuppressFinalize(this);
    }
}
