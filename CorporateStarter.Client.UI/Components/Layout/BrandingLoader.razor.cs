using CorporateStarter.Client.Core.Configuration;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CorporateStarter.Client.UI.Components.Layout
{
    public partial class BrandingLoader : IDisposable
    {
        [Inject] PreferencesClient Client {  get; set; }
        private readonly CancellationTokenSource _lifetime = new();
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;
            try { await Client.GetAsync(_lifetime.Token); }
            catch (Exception) { /* Branding is optional: keep the default and let Preferences expose Retry. */ }
        }
        public void Dispose() { _lifetime.Cancel(); _lifetime.Dispose(); }
    }
}
