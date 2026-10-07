using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Shared.Common;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CorporateStarter.Client.UI.Components.Editors;

public abstract class RecordEditorBase<TModel, TDetails> : ComponentBase, IDisposable where TModel : new()
{
    [CascadingParameter] protected IMudDialogInstance Dialog { get; set; } = default!;
    [Inject] protected IClientAuthState Auth { get; set; } = default!;
    [Inject] protected ILogger<RecordEditorBase<TModel, TDetails>> Logger { get; set; } = default!;
    private readonly CancellationTokenSource _lifetime = new();
    protected abstract Guid? RecordId { get; }
    protected bool IsEditing => RecordId.HasValue;
    protected TModel _model = new();
    protected TableCapabilities _capabilities = new(false, false, false, false, false);
    protected abstract bool IsDirty { get; }
    protected abstract Task<TableCapabilities> LoadCapabilitiesAsync(CancellationToken ct);
    protected abstract Task<TModel> ReadModelAsync(Guid id, CancellationToken ct);
    protected abstract Task<TDetails> WriteModelAsync(CancellationToken ct);
    protected abstract void CaptureBaseline();
    protected abstract string ConflictMessage { get; }
    protected virtual string SaveNotFoundMessage => "This record no longer exists. Close the form and refresh the list.";
    protected bool _loadingDetails;
    protected bool _loaded;
    protected EditorDialogFrame? _frame;
    private ClientAuthSnapshot _initial = default!;
    protected bool _sessionReady, _saving, _disposed, _closed, _uncertain, _confirmDiscard;
    protected string? _error;
    protected bool Blocked => _saving || !_sessionReady || _uncertain || _closed || !_loaded || _loadingDetails;
    protected static string? Required(string? value) => string.IsNullOrWhiteSpace(value) ? "This field is required." : null;

    protected override async Task OnInitializedAsync()
    {
        _initial = Auth.Current;
        _sessionReady = _initial.Status == ClientAuthStatus.Authenticated;
        Auth.StateChanged += OnSessionChanged;
        if (!_sessionReady) { ForceClose(); return; }
        try { _capabilities = await LoadCapabilitiesAsync(_lifetime.Token); }
        catch (Exception ex) { Logger.LogWarning(ex, "Could not read record permissions."); ForceClose(); return; }
        if (_closed || _disposed) return;
        if (IsEditing ? !_capabilities.CanUpdate : !_capabilities.CanCreate) { ForceClose(); return; }
        if (IsEditing) await LoadDetailsAsync();
        else { CaptureBaseline(); _loaded = true; }
    }

    protected async Task LoadDetailsAsync()
    {
        if (_closed || _disposed || _loadingDetails || RecordId is not { } id) return;
        _loadingDetails = true;
        _error = null;
        try
        {
            var model = await ReadModelAsync(id, _lifetime.Token);
            if (_closed || _disposed) return;
            _model = model;
            CaptureBaseline();
            _loaded = true;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (ClientApiHttpException ex)
        {
            _error = ex.StatusCode switch
            {
                404 => "This record no longer exists. Close the form and refresh the list.",
                403 => "You do not have permission to read this record.",
                _ => "Could not load the record. Please retry."
            };
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Loading the record editor failed.");
            _error = "Could not load the record. Please retry after session verification.";
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

    protected async Task SaveAsync()
    {
        if (Blocked || _confirmDiscard || _frame is null) return;
        // Set before the first await to prevent concurrent submissions.
        _saving = true;
        try
        {
            var valid = await _frame.ValidateAsync();
            if (_closed || _disposed || !_sessionReady || !valid) return;
            _error = null;
            var saved = await WriteModelAsync(_lifetime.Token);
            if (_closed || _disposed) return;
            _closed = true;
            _model = new();
            Dialog.Close(DialogResult.Ok(saved));
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (ClientApiHttpException ex)
        {
            if (ex.StatusCode >= 500) MarkUncertain();
            else _error = CorporateStarter.Client.Core.Api.CommandErrorMessages.ForCode(ex.ErrorCode) ?? ex.StatusCode switch
            {
                400 => "Check the record fields and try again.",
                401 => "The request was not authorized. Verify your session before saving again.",
                403 => "You do not have permission to save this record.",
                404 => SaveNotFoundMessage,
                409 => ConflictMessage,
                429 => "Too many requests. Please wait before saving again.",
                _ => "The service rejected the request. Check the record list before trying again."
            };
        }
        catch (ClientApiSessionException) { MarkUncertain(); }
        catch (ClientApiTransportException) { MarkUncertain(); }
        catch (InvalidDataException) { MarkUncertain(); }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Saving the record could not be confirmed.");
            MarkUncertain();
        }
        finally { _saving = false; }
    }

    private void MarkUncertain()
    {
        _uncertain = true;
        _error = "Saving could not be confirmed. Close this form and check the record list before trying again.";
    }
    protected static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    protected Task CloseAsync()
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
    protected void KeepEditing() => _confirmDiscard = false;
    protected void Discard() => ForceClose();
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
