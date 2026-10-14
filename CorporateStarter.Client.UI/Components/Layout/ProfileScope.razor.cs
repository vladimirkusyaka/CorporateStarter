using CorporateStarter.Client.Core.Auth;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CorporateStarter.Client.UI.Components.Layout
{
    public partial class ProfileScope : IDisposable
    {
        [Inject] ProfileClient Client { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        private readonly CancellationTokenSource _lifetime = new();
        private bool _loaded, _error, _busy;
        protected override Task OnAfterRenderAsync(bool firstRender) => firstRender ? LoadAsync() : Task.CompletedTask;
        private async Task LoadAsync()
        {
            _busy = true; _error = false;
            try { await Client.GetAsync(_lifetime.Token); _loaded = true; }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
            catch (Exception) { _error = true; }
            finally { _busy = false; if (!_lifetime.IsCancellationRequested) StateHasChanged(); }
        }
        public void Dispose() { _lifetime.Cancel(); _lifetime.Dispose(); }
    }
}
