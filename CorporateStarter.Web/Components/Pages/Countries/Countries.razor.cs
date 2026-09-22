using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Core.MasterData.Countries;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
using Microsoft.AspNetCore.Components;

namespace CorporateStarter.Web.Components.Pages.Countries;

public partial class Countries : IDisposable
{
    [Inject]
    private CountriesClient Client { get; set; } = default!;

    [Inject]
    private ILogger<Countries> Logger { get; set; } = default!;

    private static readonly int[] PageSizes = [10, 20, 50];
    private readonly CancellationTokenSource _lifetime = new();
    private IReadOnlyList<CountryListItemDto> _countries = [];
    private bool _loadStarted;
    private bool _loading;
    private bool _disposed;
    private string? _error;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await LoadAsync();
        else
            ApplyPendingSelection();
    }

    protected async Task LoadAsync()
    {
        if (_disposed || _loading)
            return;

        var cancellationToken = _lifetime.Token;
        ClearSelection();
        ResetSearchPosition();
        _loadStarted = true;
        _loading = true;
        _error = null;
        _countries = [];
        StateHasChanged();

        try
        {
            _capabilities = await Client.CapabilitiesAsync(cancellationToken);
            if (!_capabilities.ViewInactive) { _filters.Remove(CountryColumn.Status); if (_sortColumn == CountryColumn.Status) _sortColumn = null; }
            var page = await Client.QueryAsync(BuildQuery(), cancellationToken);
            if (!_disposed && !cancellationToken.IsCancellationRequested)
            {
                _countries = page.Items;
                _totalCount = page.TotalCount;
                _currentPage = page.Page - 1;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ClientApiHttpException exception)
        {
            _error = exception.StatusCode switch
            {
                401 => "The request could not be authorized. Retry after session verification.",
                403 => "You do not have permission to view countries.",
                404 => "The countries service could not be found.",
                429 => "Too many requests. Please wait before retrying.",
                >= 500 => "The countries service is temporarily unavailable. Please retry.",
                _ => "The countries service rejected the request. Please retry."
            };
        }
        catch (ClientApiSessionException exception)
        {
            _error = exception.Failure switch
            {
                ClientApiSessionFailure.Busy =>
                    "Session verification is in progress. Retry when it finishes.",
                ClientApiSessionFailure.SessionChanged =>
                    "The session changed. Reload the country list.",
                _ => "A valid sign-in is required to load countries."
            };
        }
        catch (ClientApiTransportException)
        {
            _error = "Could not receive a response from the service. Please retry.";
        }
        catch (InvalidDataException)
        {
            Logger.LogWarning("The countries service returned an invalid response.");
            _error = "The service returned an unexpected response. Please contact support if this persists.";
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Loading the country list failed.");
            _error = "Could not load countries. Please retry.";
        }
        finally
        {
            _loading = false;
            if (!_disposed)
                StateHasChanged();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _createDialog?.Close();
        _lifetime.Cancel();
        _lifetime.Dispose();
        _countries = [];
        _error = null;
        GC.SuppressFinalize(this);
    }
}
