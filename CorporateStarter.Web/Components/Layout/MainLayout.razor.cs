using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace CorporateStarter.Web.Components.Layout
{
    public partial class MainLayout
    {
        protected const string PageTitle = "CorporateStarter";

        protected const string BuildVersion = "DEV 1.0.0.0";

        protected bool DrawerOpen { get; set; } = true;

        protected bool IsAuthenticated { get; set; }

        private MudTextField<string>? _usernameField;

        protected bool IsSigningIn { get; set; }

        protected string Login { get; set; } = string.Empty;

        protected string Password { get; set; } = string.Empty;

        protected string? SignInError { get; set; }

        protected string CurrentUserName { get; set; } = "guest";

        protected string CurrentUserInitials { get; set; } = "G";

        protected int UnreadNotifications { get; set; } = 1;

        protected MudTheme AppTheme { get; } = new()
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#0d6efd",
                Secondary = "#6b7280",
                AppbarBackground = "#ffffff",
                AppbarText = "#111827",
                DrawerBackground = "#ffffff",
                Background = "#f4f5f7",
                Surface = "#ffffff",
                TextPrimary = "#111827",
                TextSecondary = "#6b7280",
                Divider = "#d7dde5"
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "5px"
            }
        };

        protected override async Task OnInitializedAsync()
        {
            await CheckSessionAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !IsAuthenticated && _usernameField is not null)
            {
                await _usernameField.FocusAsync();
            }
        }

        protected void ToggleDrawer()
        {
            DrawerOpen = !DrawerOpen;
        }

        protected async Task OnLoginKeyDown(KeyboardEventArgs args)
        {
            if (args.Key == "Enter")
            {
                await SignInAsync();
            }
        }

        protected async Task SignInAsync()
        {
            SignInError = null;

            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                SignInError = "Enter username and password.";
                return;
            }

            IsSigningIn = true;

            try
            {
                // Visual scaffold only. Wire this to the real auth flow before production use.
                await Task.Delay(250);

                IsAuthenticated = true;
                CurrentUserName = Login.Trim();
                CurrentUserInitials = BuildInitials(CurrentUserName);
                Password = string.Empty;
            }
            finally
            {
                IsSigningIn = false;
            }
        }

        protected async Task SignOutAsync()
        {
            // Visual scaffold only. Later: call API logout and clear server-side UI session state.
            await Task.CompletedTask;

            IsAuthenticated = false;
            CurrentUserName = "guest";
            CurrentUserInitials = "G";
            Password = string.Empty;
        }

        private async Task CheckSessionAsync()
        {
            // Later: call refresh/me flow. If it fails, leave the shell locked.
            await Task.CompletedTask;

            IsAuthenticated = false;
        }

        private static string BuildInitials(string value)
        {
            var trimmed = value.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
                return "U";

            var parts = trimmed
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length >= 2)
                return string.Concat(parts[0][0], parts[1][0]).ToUpperInvariant();

            return trimmed[0].ToString().ToUpperInvariant();
        }
    }
}
