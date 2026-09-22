using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Web.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace CorporateStarter.Web.Components.Layout;

public partial class MainLayout
{
    [Inject] private IOptions<SessionUiOptions> SessionUi { get; set; } = default!;
    private CancellationTokenSource? _sessionFeedbackCancellation;
    private bool _showSessionCheckFeedback;

    private bool IsSessionCheck => !Session.IsLogoutPending &&
        _snapshot.Status is ClientAuthStatus.Initializing or ClientAuthStatus.Revalidating;

    // This preserves already displayed content, never authenticates a user or mounts a new page.
    private bool CanKeepContentDuringCheck => IsConnectionReady && !Session.IsLogoutPending &&
        _snapshot.Status == ClientAuthStatus.Revalidating &&
        _snapshot.UserId is not null && _authorizedBody is not null;

    private bool ShowAuthorizedContent => IsAuthenticated || CanKeepContentDuringCheck;
    private bool IsQuietSessionCheck => CanKeepContentDuringCheck && !_showSessionCheckFeedback;
    private bool ShowSessionOverlay => !IsAuthenticated && !ShowLogin &&
        (!IsSessionCheck || _showSessionCheckFeedback);

    private void UpdateSessionFeedback()
    {
        if (_disposed || !IsSessionCheck || ShowLogin)
        {
            CancelSessionFeedback();
            return;
        }

        // A revision change within the same check must not restart the delay.
        if (_sessionFeedbackCancellation is not null) return;
        var seconds = SessionUi.Value.CheckIndicatorDelaySeconds;
        if (seconds == 0)
        {
            _showSessionCheckFeedback = true;
            return;
        }

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _sessionFeedbackCancellation = cancellation;
        _ = RevealSessionFeedbackAsync(cancellation, TimeSpan.FromSeconds(seconds));
    }

    private async Task RevealSessionFeedbackAsync(CancellationTokenSource cancellation, TimeSpan delay)
    {
        var token = cancellation.Token;
        try
        {
            await Task.Delay(delay, token);
            await InvokeAsync(() =>
            {
                if (_disposed || token.IsCancellationRequested ||
                    !ReferenceEquals(_sessionFeedbackCancellation, cancellation) ||
                    !IsSessionCheck || ShowLogin) return;
                _showSessionCheckFeedback = true;
                StateHasChanged();
            });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception)
        {
            if (!_disposed) Logger.LogError(exception, "Delayed session feedback failed.");
        }
    }

    private void CancelSessionFeedback()
    {
        var cancellation = _sessionFeedbackCancellation;
        _sessionFeedbackCancellation = null;
        _showSessionCheckFeedback = false;
        if (cancellation is null) return;
        cancellation.Cancel();
        cancellation.Dispose();
    }
}
