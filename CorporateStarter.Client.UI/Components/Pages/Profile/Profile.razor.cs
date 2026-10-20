using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Components;
namespace CorporateStarter.Client.UI.Components.Pages.Profile;

public partial class Profile : IDisposable
{
    [Inject] private ProfileClient Client { get; set; } = default!;
    [Inject] private ClientSessionCoordinator Session { get; set; } = default!;
    private readonly CancellationTokenSource _lifetime = new();
    private MyProfileDto? _profile;
    private string? _displayName, _error, _message;
    private string _theme = "System", _timeZone = "UTC";
    private int _pageSize = 20, _tab;
    private string _currentPassword = "", _newPassword = "", _confirmation = "";
    private bool _busy, _disposed, _passwordUncertain;
    protected override Task OnAfterRenderAsync(bool firstRender) => firstRender ? LoadAsync() : Task.CompletedTask;
    private async Task LoadAsync()
    {
        if (_busy) return;
        _busy = true; _error = null;
        try
        {
            var profile = await Client.GetAsync(_lifetime.Token);
            if (_disposed) return;
            _profile = profile; _displayName = profile.Account.DisplayName;
            _theme = profile.Appearance.Theme; _pageSize = profile.Appearance.PageSize; _timeZone = profile.Appearance.TimeZone;
        }
        catch (OperationCanceledException) when (_disposed) { }
        catch (Exception) { _error = "Could not load your profile. Please retry."; }
        finally { _busy = false; if (!_disposed) StateHasChanged(); }
    }
    private Task SaveAccountAsync() => SaveAsync(() => Client.SaveAccountAsync(_displayName, _lifetime.Token), "Account saved.");
    private Task SaveAppearanceAsync() => SaveAsync(() => Client.SaveAppearanceAsync(new() { Theme = _theme, PageSize = _pageSize, TimeZone = _timeZone, Language = L.Language }, _lifetime.Token), "Appearance saved.");
    private async Task SaveAsync(Func<Task> save, string message)
    {
        if (_busy) return;
        _busy = true; _error = null; _message = null;
        try { await save(); if (!_disposed) _message = message; }
        catch (OperationCanceledException) when (_disposed) { }
        catch (Exception) { _error = "Saving was not confirmed. Reopen Profile to check the saved values before trying again."; }
        finally { _busy = false; }
    }
    private async Task ChangePasswordAsync()
    {
        if (_busy || _passwordUncertain) return;
        _message = null; _error = null;
        if (string.IsNullOrEmpty(_currentPassword) || string.IsNullOrEmpty(_newPassword)) { _error = "Enter your current and new passwords."; return; }
        if (_newPassword != _confirmation) { _error = "New passwords do not match."; return; }
        _busy = true;
        try
        {
            await Client.ChangePasswordAsync(new() { CurrentPassword = _currentPassword, NewPassword = _newPassword }, _lifetime.Token);
            _message = "Password changed. Sign in with your new password.";
            await Session.LogoutAsync();
        }
        catch (OperationCanceledException) when (_disposed) { }
        catch (ClientApiHttpException ex) when (ex.StatusCode is 400 or 409 or 429)
        {
            _error = ex.ErrorCode switch
            {
                "profile.current_password_invalid" => "The current password is incorrect.",
                "profile.password_policy" => "The new password does not meet the password policy.",
                "profile.password_reused" => "Choose a password you have not used recently.",
                "profile.concurrent_change" => "Your account changed during this request. Check your session and try again.",
                _ => ex.StatusCode == 429 ? "Too many attempts. Please wait before trying again." : "The password change was rejected. Check your entries."
            };
        }
        catch (Exception)
        {
            _passwordUncertain = true;
            _error = "The password change could not be confirmed. Sign out, then try signing in with the new password. If it was not changed, use the previous password.";
        }
        finally { _currentPassword = _newPassword = _confirmation = ""; _busy = false; }
    }
    private Task SignOutAsync() => Session.LogoutAsync();
    public void Dispose() { _disposed = true; _currentPassword = _newPassword = _confirmation = ""; _lifetime.Cancel(); _lifetime.Dispose(); }
}
