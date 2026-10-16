using CorporateStarter.Client.Core.Auth;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CorporateStarter.Client.UI.Components.Layout
{
    public partial class PermissionNavigation: IDisposable
    {
        [Inject] ClientPermissions Permissions { get; set; }
        [Parameter] public bool Expanded { get; set; }
        protected override void OnInitialized() => Permissions.Changed += OnChanged;
        private void OnChanged() => _ = InvokeAsync(StateHasChanged);
        public void Dispose() => Permissions.Changed -= OnChanged;
        private static string Icon(string name) => name switch
        {
            "Business" => Icons.Material.Filled.Business,
            "Apartment" => Icons.Material.Filled.Apartment,
            "People" => Icons.Material.Filled.People,
            "Storage" => Icons.Material.Filled.Storage,
            "Public" => Icons.Material.Filled.Public,
            "LocationCity" => Icons.Material.Filled.LocationCity,
            "Badge" => Icons.Material.Filled.Badge,
            "ManageAccounts" => Icons.Material.Filled.ManageAccounts,
            "AdminPanelSettings" => Icons.Material.Filled.AdminPanelSettings,
            "Key" => Icons.Material.Filled.Key,
            "History" => Icons.Material.Filled.History,
            "Feed" => Icons.Material.Filled.Feed,
            _ => Icons.Material.Filled.Security
        };
    }
}
