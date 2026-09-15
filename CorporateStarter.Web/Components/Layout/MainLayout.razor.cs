using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace CorporateStarter.Web.Components.Layout
{
    public partial class MainLayout : IDisposable
    {
        [Inject]
        private IClientAuthState AuthState { get; set; } = default!;

        [Inject]
        private ClientSessionCoordinator Session { get; set; } = default!;

        protected const string PageTitle = "CorporateStarter";
        protected const string BuildVersion = "DEV 1.0.0.0";

        protected bool DrawerOpen { get; set; } = true;

        protected bool IsAuthenticated =>
            _snapshot.Status == ClientAuthStatus.Authenticated;

        protected bool ShowLogin =>
            !Session.IsLogoutPending &&
            (_snapshot.Status == ClientAuthStatus.Anonymous ||
            (IsSigningIn &&
            _snapshot.Status == ClientAuthStatus.Revalidating));

        protected bool IsSigningIn { get; set; }
        protected string Login { get; set; } = string.Empty;
        protected string Password { get; set; } = string.Empty;
        protected string? SignInError { get; set; }

        protected string CurrentUserName =>
            _snapshot.UserId is null ? "guest" : "Signed in";

        protected string CurrentUserInitials =>
            _snapshot.UserId is null ? "G" : "U";

        protected int UnreadNotifications { get; set; } = 1;

        private ClientAuthSnapshot _snapshot = ClientAuthSnapshot.Initial;
        private readonly CancellationTokenSource _lifetime = new();
        private MudTextField<string>? _usernameField;
        private RenderFragment? _authorizedBody;
        private bool _restoreInProgress;
        private bool _focusLogin;
        private bool _disposed;
        private bool _signingOut;

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

        protected override void OnInitialized()
        {
            AuthState.StateChanged += OnAuthStateChanged;
            ApplyCurrentState();
        }

        protected override void OnParametersSet()
        {
            if (IsAuthenticated)
                _authorizedBody = Body;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender &&
                _snapshot.Status == ClientAuthStatus.Initializing)
            {
                await RestoreSessionAsync();
            }

            if (!_disposed && _focusLogin && ShowLogin &&
                !IsSigningIn && _usernameField is not null)
            {
                _focusLogin = false;

                try
                {
                    await _usernameField.FocusAsync();
                }
                catch (JSDisconnectedException)
                {
                }
            }
        }

        private void OnAuthStateChanged(
            object? sender,
            ClientAuthStateChangedEventArgs args)
        {
            if (_disposed)
                return;

            _ = InvokeAsync(() =>
            {
                if (_disposed)
                    return;

                ApplyCurrentState();
                StateHasChanged();
            });
        }

        private void ApplyCurrentState()
        {
            var next = AuthState.Current;

            if (next.SessionGeneration != _snapshot.SessionGeneration)
            {
                Login = string.Empty;
                Password = string.Empty;
                SignInError = null;
                _authorizedBody = null;
            }

            if (next.Status == ClientAuthStatus.Anonymous &&
                _snapshot.Status != ClientAuthStatus.Anonymous)
            {
                _focusLogin = true;
            }

            _snapshot = next;

            if (IsAuthenticated)
                _authorizedBody = Body;
            else if (_snapshot.UserId is null)
                _authorizedBody = null;
        }

        protected async Task RestoreSessionAsync()
        {
            if (_disposed || _restoreInProgress || IsSigningIn)
                return;

            _restoreInProgress = true;

            try
            {
                await Session.RestoreAsync(_lifetime.Token);
            }
            catch (OperationCanceledException)
                when (_lifetime.IsCancellationRequested)
            {
            }
            finally
            {
                _restoreInProgress = false;

                if (!_disposed)
                {
                    ApplyCurrentState();
                    StateHasChanged();
                }
            }
        }

        protected async Task SignInAsync()
        {
            if (_disposed || IsSigningIn ||
                AuthState.Current.Status != ClientAuthStatus.Anonymous)
            {
                return;
            }

            SignInError = null;
            IsSigningIn = true;

            var password = Password;
            Password = string.Empty;

            try
            {
                var result = await Session.LoginAsync(Login, password);

                if (_disposed || result is null)
                    return;

                ApplyCurrentState();

                if (_snapshot.Status == ClientAuthStatus.Anonymous)
                {
                    SignInError = result.Status switch
                    {
                        ClientLoginStatus.Rejected =>
                            "Sign-in failed. Check your credentials or try again later.",

                        ClientLoginStatus.InvalidInput =>
                            "Enter username and password.",

                        ClientLoginStatus.RateLimited =>
                            "Too many attempts. Try again later.",

                        ClientLoginStatus.MfaRequired =>
                            "Additional verification is required. This sign-in screen does not support it yet.",

                        _ => null
                    };
                }
            }
            finally
            {
                IsSigningIn = false;
            }
        }

        protected void ToggleDrawer() => DrawerOpen = !DrawerOpen;

        protected Task OnLoginKeyDown(KeyboardEventArgs args) =>
            args.Key == "Enter"
                ? SignInAsync()
                : Task.CompletedTask;

        protected async Task SignOutAsync()
        {
            if (_disposed || _signingOut ||
                _restoreInProgress || IsSigningIn)
            {
                return;
            }

            _signingOut = true;

            try
            {
                await Session.LogoutAsync();
            }
            finally
            {
                _signingOut = false;

                if (!_disposed)
                {
                    ApplyCurrentState();
                    StateHasChanged();
                }
            }
        }

        protected Task RetrySessionAsync() =>
            Session.IsLogoutPending
                ? SignOutAsync()
                : RestoreSessionAsync();

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            AuthState.StateChanged -= OnAuthStateChanged;
            _authorizedBody = null;
            Password = string.Empty;
            _lifetime.Cancel();
            _lifetime.Dispose();
        }
    }
}
