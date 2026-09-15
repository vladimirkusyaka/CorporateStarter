using CorporateStarter.Client.Browser.auth;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace CorporateStarter.Client.Browser.Components;

public partial class BrowserEnvironmentGate : IDisposable
{
    [Inject]
    private BrowserCapabilityProbe Probe { get; set; } = default!;

    [Inject]
    private ILogger<BrowserEnvironmentGate> Logger { get; set; } = default!;

    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    private GateStatus _status = GateStatus.Checking;
    private bool _disposed;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await CheckAsync();
    }

    private Task RetryAsync() =>
        !_disposed && _status == GateStatus.Unavailable
            ? CheckAsync()
            : Task.CompletedTask;

    private async Task CheckAsync()
    {
        _status = GateStatus.Checking;

        using var timeout =
            new CancellationTokenSource(TimeSpan.FromSeconds(15));

        GateStatus result;

        try
        {
            var capabilities = await Probe.CheckAsync(timeout.Token);

            result = capabilities.IsSupported
                ? GateStatus.Ready
                : GateStatus.Unsupported;
        }
        catch (JSDisconnectedException)
        {
            result = GateStatus.Unavailable;
        }
        catch (JSException exception)
        {
            Logger.LogWarning(
                exception,
                "Browser capability check failed.");

            result = GateStatus.Unavailable;
        }
        catch (OperationCanceledException)
            when (timeout.IsCancellationRequested)
        {
            Logger.LogWarning("Browser capability check timed out.");
            result = GateStatus.Unavailable;
        }

        if (_disposed)
            return;

        _status = result;
        StateHasChanged();
    }

    public void Dispose() => _disposed = true;

    private enum GateStatus
    {
        Checking,
        Ready,
        Unsupported,
        Unavailable
    }
}