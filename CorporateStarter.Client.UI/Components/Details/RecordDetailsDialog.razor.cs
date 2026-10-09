using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using CorporateStarter.Shared.Common;
using System.Collections.Generic;
using System.Text;
using static MudBlazor.CategoryTypes;


namespace CorporateStarter.Client.UI.Components.Details
{
    public partial class RecordDetailsDialog<TItem>
    {
        [Parameter] public string Title { get; set; } = "Details";
        [Parameter, EditorRequired] public Func<CancellationToken, Task<TItem>> Load { get; set; } = default!;
        [Parameter, EditorRequired] public RenderFragment<TItem> ChildContent { get; set; } = default!;
        [Inject] private IClientAuthState Auth { get; set; } = default!;
        [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;
        private readonly CancellationTokenSource _lifetime = new();
        private ClientAuthSnapshot _initial = default!;
        private TItem? _item;
        private bool _loading, _loaded, _disposed, _closed;
        private string? _error;
        protected override async Task OnInitializedAsync()
        {
            _initial = Auth.Current;
            Auth.StateChanged += SessionChanged;
            if (_initial.Status != ClientAuthStatus.Authenticated) { Close(); return; }
            await LoadAsync();
        }
        private async Task LoadAsync()
        {
            if (_loading || _closed || _disposed) return;
            _loading = true; _loaded = false; _item = default; _error = null;
            try
            {
                var item = await Load(_lifetime.Token);
                if (!_closed && !_disposed) { _item = item; _loaded = true; }
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
            catch (ClientApiHttpException ex)
            {
                _error = ex.StatusCode switch { 403 => "You do not have permission to view this record.", 404 => "This record is no longer available.", _ => "Could not load the record. Please retry." };
            }
            catch (Exception) { _error = "Could not load the record. Retry after session verification."; }
            finally { _loading = false; }
        }
        private void SessionChanged(object? sender, ClientAuthStateChangedEventArgs args) => _ = InvokeAsync(() =>
        {
            if (_closed || _disposed) return;
            var current = Auth.Current;
            if (current.UserId != _initial.UserId || current.SessionGeneration != _initial.SessionGeneration || current.Status != ClientAuthStatus.Authenticated) Close();
        });
        private void Close()
        {
            if (_closed) return;
            _closed = true; _item = default; _loaded = false; _lifetime.Cancel(); Dialog.Cancel();
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true; Auth.StateChanged -= SessionChanged; _lifetime.Cancel(); _lifetime.Dispose(); _item = default;
        }
    }
}
