using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CorporateStarter.Client.UI.Components.Layout
{
    public partial class UserMenu : IDisposable
    {
        [Inject] IClientAuthState Auth { get; set; }
        [Inject] CurrentUserClient Client { get; set; }
        [Parameter] public bool LogoutDisabled { get; set; }
        [Parameter] public EventCallback Logout { get; set; }
        private UserProfileDto? _profile;
        private ClientAuthSnapshot _snapshot = ClientAuthSnapshot.Initial;
        private long? _attemptedGeneration;
        private CancellationTokenSource? _request;
        private bool _disposed;
        private string Name => _profile is null ? "Signed in" : CurrentUserClient.Name(_profile);
        private string Initial => _profile is null ? "?" : CurrentUserClient.Initial(Name);
        protected override void OnInitialized()
        {
            _snapshot = Auth.Current;
            Auth.StateChanged += Changed;
        }
        protected override Task OnAfterRenderAsync(bool firstRender) => firstRender ? LoadAsync() : Task.CompletedTask;
        private void Changed(object? sender, ClientAuthStateChangedEventArgs args) => _ = InvokeAsync(async () =>
        {
            if (_disposed) return;
            var current = Auth.Current;
            if (current.UserId != _snapshot.UserId || current.SessionGeneration != _snapshot.SessionGeneration)
            {
                _request?.Cancel(); _profile = null; _attemptedGeneration = null;
            }
            _snapshot = current;
            StateHasChanged();
            await LoadAsync();
        });
        private async Task LoadAsync()
        {
            var snapshot = Auth.Current;
            if (_disposed || snapshot.Status != ClientAuthStatus.Authenticated || snapshot.UserId is not { } id || _attemptedGeneration == snapshot.SessionGeneration) return;
            _attemptedGeneration = snapshot.SessionGeneration;
            using var request = new CancellationTokenSource();
            _request = request;
            try
            {
                var profile = await Client.GetAsync(id, request.Token);
                var current = Auth.Current;
                if (!_disposed && !request.IsCancellationRequested && current.UserId == id && current.SessionGeneration == snapshot.SessionGeneration)
                    _profile = profile;
            }
            catch (OperationCanceledException) when (request.IsCancellationRequested) { }
            catch (Exception) { /* The menu and Logout remain usable if the profile cannot be read. */ }
            finally
            {
                if (ReferenceEquals(_request, request)) _request = null;
                if (!_disposed) StateHasChanged();
            }
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true; Auth.StateChanged -= Changed; _request?.Cancel(); _profile = null;
        }
    }
}
