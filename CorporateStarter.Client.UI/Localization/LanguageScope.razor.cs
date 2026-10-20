using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.UI.Localization
{
    public partial class LanguageScope: IAsyncDisposable
    {
        [Inject] IJSRuntime Js { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        private IJSObjectReference? _module;
        private bool _ready, _disposed;
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;
            try
            {
                _module = await Js.InvokeAsync<IJSObjectReference>("import", "./_content/CorporateStarter.Client.UI/localization.js?v=20261017");
                var language = await _module.InvokeAsync<string>("initialize");
                if (_disposed) return;
                L.Select(CorporateStarter.Shared.Localization.UiText.IsSupported(language) ? language : "en-US");
                await _module.InvokeVoidAsync("write", L.Language);
            }
            catch (JSException) { }
            if (_disposed) return;
            L.Changed += Changed;
            _ready = true; StateHasChanged();
        }
        private void Changed() => _ = InvokeAsync(async () =>
        {
            if (_disposed) return;
            StateHasChanged();
            try { if (_module is not null) await _module.InvokeVoidAsync("write", L.Language); }
            catch (JSException) { }
        });
        public async ValueTask DisposeAsync()
        {
            _disposed = true; L.Changed -= Changed;
            try { if (_module is not null) await _module.DisposeAsync(); }
            catch (JSException) { }
        }

    }
}
