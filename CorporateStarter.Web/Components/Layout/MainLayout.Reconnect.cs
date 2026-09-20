using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Web.Services.Auth;
using Microsoft.AspNetCore.Components;

namespace CorporateStarter.Web.Components.Layout;

public partial class MainLayout
{
    [Inject]
    private CircuitConnectionMonitor Connection { get; set; } = default!;

    [Inject]
    private ILogger<MainLayout> Logger { get; set; } = default!;

    private readonly string _connectionScopeId = Guid.NewGuid().ToString("N");
    private long _connectionRevision;
    private long _verifiedConnectionRevision;
    private long _connectionCheckRevision = -1;
    private bool _browserReady;

    protected string ConnectionStamp =>
        $"{_connectionScopeId}:{Connection.Current.Revision}";

    protected bool IsConnectionReady
    {
        get
        {
            var current = Connection.Current;
            return current.IsConnected && current.Revision == _verifiedConnectionRevision;
        }
    }

    protected bool CanDismissReconnect => IsConnectionReady &&
        _snapshot.Status is not (ClientAuthStatus.Initializing or ClientAuthStatus.Revalidating);

    private void InitializeConnectionMonitoring()
    {
        _connectionRevision = Connection.Current.Revision;
        _verifiedConnectionRevision = _connectionRevision;
        Connection.Changed += OnConnectionChanged;
    }

    private void OnConnectionChanged()
    {
        if (_disposed)
            return;

        var current = Connection.Current;
        if (current.Revision == _connectionRevision)
            return;

        _connectionRevision = current.Revision;
        _verifiedConnectionRevision = -1;
        Session.SuspendForReconnect();
        _ = DispatchConnectionChangeAsync(current.Revision);
    }

    private async Task DispatchConnectionChangeAsync(long revision)
    {
        try
        {
            await InvokeAsync(() => RevalidateConnectionAsync(revision));
        }
        catch (Exception exception)
        {
            if (!_disposed)
                Logger.LogError(exception, "Circuit session verification failed.");
        }
    }

    private async Task RevalidateConnectionAsync(long revision)
    {
        if (_disposed)
            return;

        ApplyCurrentState();
        StateHasChanged();

        var current = Connection.Current;
        if (!_browserReady || !current.IsConnected || current.Revision != revision ||
            _connectionCheckRevision == revision)
            return;

        _connectionCheckRevision = revision;
        var cancellationToken = _lifetime.Token;

        try
        {
            await Session.RestoreAfterReconnectAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (!_disposed)
            {
                current = Connection.Current;
                if (current.IsConnected && current.Revision == revision)
                    _verifiedConnectionRevision = revision;

                ApplyCurrentState();
                StateHasChanged();
            }
        }
    }
}
