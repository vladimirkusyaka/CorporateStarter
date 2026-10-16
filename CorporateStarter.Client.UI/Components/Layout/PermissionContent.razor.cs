using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.UI.Components.Pages.Permissions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CorporateStarter.Client.UI.Components.Layout
{
    public partial class PermissionContent: IDisposable
    {
        [Inject] ClientPermissions Permissions { get; set; }
        [Inject] NavigationManager Navigation { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        protected override void OnInitialized()
        {
            Permissions.Changed += OnChanged;
            Navigation.LocationChanged += OnLocationChanged;
        }
        private void OnChanged() => _ = InvokeAsync(StateHasChanged);
        private void OnLocationChanged(object? sender, LocationChangedEventArgs args) => OnChanged();
        public void Dispose()
        {
            Permissions.Changed -= OnChanged;
            Navigation.LocationChanged -= OnLocationChanged;
        }
    }
}
