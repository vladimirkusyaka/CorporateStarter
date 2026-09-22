using System.ComponentModel.DataAnnotations;
using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.MasterData.Countries;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CorporateStarter.Web.Components.Pages.Countries;

public partial class CountryEditorDialog : IDisposable
{
    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;
    [Inject] private CountriesClient Client { get; set; } = default!;
    [Inject] private IClientAuthState Auth { get; set; } = default!;
    [Inject] private ILogger<CountryEditorDialog> Logger { get; set; } = default!;
    private readonly CancellationTokenSource _lifetime = new();
    [Parameter] public Guid? CountryId { get; set; }
    private bool IsEditing => CountryId.HasValue;
    private UpdateCountryRequest _model = new() { IsActive = true };
    private (string, string, string?, string?, bool?) _baseline = ("", "", null, null, true);
    private CountryCapabilities _capabilities = new(false, false, false, false, false);
    private bool CanChangeActive => _capabilities.ViewInactive && (_baseline.Item5 == true ? _capabilities.CanDelete : _capabilities.CanRestore);
    private bool ActiveValue { get => _model.IsActive ?? true; set => _model.IsActive = value; }
    private bool _loadingDetails;
    private bool _loaded;
    private MudForm? _form;
    private ClientAuthSnapshot _initial = default!;
    private bool _sessionReady, _saving, _disposed, _closed, _uncertain, _confirmDiscard;
    private string? _error;
    private bool Blocked => _saving || !_sessionReady || _uncertain || _closed || !_loaded || _loadingDetails;
    private bool IsDirty => (_model.Code, _model.Name, _model.NativeName, _model.PhoneCode, _model.IsActive) != _baseline;
    private static string? Required(string? value) => string.IsNullOrWhiteSpace(value) ? "This field is required." : null;

    private string? ValidateCode(string? value)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(_model) { MemberName = nameof(UpdateCountryRequest.Code) };
        return Validator.TryValidateProperty(value, context, results)
            ? null : results[0].ErrorMessage;
    }

    protected override async Task OnInitializedAsync()
    {
        _initial = Auth.Current;
        _sessionReady = _initial.Status == ClientAuthStatus.Authenticated;
        Auth.StateChanged += OnSessionChanged;
        if (!_sessionReady) { ForceClose(); return; }
        try { _capabilities = await Client.CapabilitiesAsync(_lifetime.Token); }
        catch (Exception ex) { Logger.LogWarning(ex, "Could not read country permissions."); ForceClose(); return; }
        if (_closed || _disposed) return;
        if (IsEditing ? !_capabilities.CanUpdate : !_capabilities.CanCreate) { ForceClose(); return; }
        if (IsEditing) await LoadDetailsAsync();
        else _loaded = true;
    }

    private async Task LoadDetailsAsync()
    {
        if (_closed || _disposed || _loadingDetails || CountryId is not { } id) return;
        _loadingDetails = true;
        _error = null;
        try
        {
            var country = await Client.GetByIdAsync(id, _lifetime.Token);
            if (_closed || _disposed) return;
            _model = new UpdateCountryRequest
            {
                Code = country.Code,
                Name = country.Name,
                NativeName = country.NativeName,
                PhoneCode = country.PhoneCode,
                IsActive = country.IsActive
            };
            _baseline = (_model.Code, _model.Name, _model.NativeName, _model.PhoneCode, _model.IsActive);
            _loaded = true;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (ClientApiHttpException ex)
        {
            _error = ex.StatusCode switch
            {
                404 => "This country no longer exists. Close the form and refresh the list.",
                403 => "You do not have permission to read this country.",
                _ => "Could not load the country. Please retry."
            };
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Loading the country editor failed.");
            _error = "Could not load the country. Please retry after session verification.";
        }
        finally { _loadingDetails = false; }
    }

    private void OnSessionChanged(object? sender, ClientAuthStateChangedEventArgs args)
    {
        _ = InvokeAsync(() =>
        {
            if (_disposed || _closed) return;
            var current = Auth.Current;
            if (current.UserId != _initial.UserId || current.SessionGeneration != _initial.SessionGeneration ||
                current.Status is ClientAuthStatus.Anonymous or ClientAuthStatus.UnsupportedEnvironment)
            {
                ForceClose();
                return;
            }
            _sessionReady = current.Status == ClientAuthStatus.Authenticated;
            StateHasChanged();
        });
    }

    private async Task SaveAsync()
    {
        if (Blocked || _confirmDiscard || _form is null) return;
        // Set before the first await to prevent concurrent submissions.
        _saving = true;
        try
        {
            await _form.ValidateAsync();
            if (_closed || _disposed || !_form.IsValid) return;
            _error = null;
            CountryDetailsDto country;
            if (CountryId is { } id)
            {
                country = await Client.UpdateAsync(id, new UpdateCountryRequest
                {
                    Code = _model.Code.Trim(),
                    Name = _model.Name.Trim(),
                    NativeName = Optional(_model.NativeName),
                    PhoneCode = Optional(_model.PhoneCode),
                    IsActive = CanChangeActive && _model.IsActive != _baseline.Item5 ? _model.IsActive : null
                }, _lifetime.Token);
            }
            else
            {
                country = await Client.CreateAsync(new CreateCountryRequest
                {
                    Code = _model.Code.Trim(),
                    Name = _model.Name.Trim(),
                    NativeName = Optional(_model.NativeName),
                    PhoneCode = Optional(_model.PhoneCode)
                }, _lifetime.Token);
            }
            if (_closed || _disposed) return;
            _closed = true;
            _model = new();
            Dialog.Close(DialogResult.Ok(country));
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (ClientApiHttpException ex)
        {
            if (ex.StatusCode >= 500) MarkUncertain();
            else _error = ex.StatusCode switch
            {
                400 => "Check the country fields and try again.",
                401 => "The request was not authorized. Verify your session before saving again.",
                403 => "You do not have permission to save this country.",
                404 => "This country no longer exists. Close the form and refresh the list.",
                409 => "A country with this code or name already exists.",
                429 => "Too many requests. Please wait before saving again.",
                _ => "The service rejected the request. Check the country list before trying again."
            };
        }
        catch (ClientApiSessionException) { MarkUncertain(); }
        catch (ClientApiTransportException) { MarkUncertain(); }
        catch (InvalidDataException) { MarkUncertain(); }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Saving the country could not be confirmed.");
            MarkUncertain();
        }
        finally { _saving = false; }
    }

    private void MarkUncertain()
    {
        _uncertain = true;
        _error = "Saving could not be confirmed. Close this form and check the country list before trying again.";
    }
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private Task CloseAsync()
    {
        if (_saving || _closed) return Task.CompletedTask;
        if (_uncertain)
        {
            _closed = true;
            _model = new();
            Dialog.Close(DialogResult.Ok<object?>(null));
        }
        else if (IsDirty) _confirmDiscard = true;
        else ForceClose();
        return Task.CompletedTask;
    }
    private void KeepEditing() => _confirmDiscard = false;
    private void Discard() => ForceClose();
    private void ForceClose()
    {
        if (_closed) return;
        _closed = true;
        _lifetime.Cancel();
        _model = new();
        _error = null;
        Dialog.Cancel();
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Auth.StateChanged -= OnSessionChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _model = new();
        GC.SuppressFinalize(this);
    }
}
