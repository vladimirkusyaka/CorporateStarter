using Microsoft.AspNetCore.Components.Server.Circuits;
using CorporateStarter.Client.Abstractions.Connection;

namespace CorporateStarter.Web.Services.Auth;

public sealed class CircuitConnectionMonitor : CircuitHandler, IClientConnectionState
{
    private readonly object _gate = new();
    private bool _isConnected;
    private long _revision;

    public event Action? Changed;

    public (bool IsConnected, long Revision) Current
    {
        get
        {
            lock (_gate)
                return (_isConnected, _revision);
        }
    }

    public override Task OnConnectionUpAsync(
        Circuit circuit,
        CancellationToken cancellationToken)
    {
        SetConnected(true);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(
        Circuit circuit,
        CancellationToken cancellationToken)
    {
        SetConnected(false);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(
        Circuit circuit,
        CancellationToken cancellationToken)
    {
        SetConnected(false);
        return Task.CompletedTask;
    }

    private void SetConnected(bool connected)
    {
        lock (_gate)
        {
            if (_isConnected == connected)
                return;

            _revision = checked(_revision + 1);
            _isConnected = connected;
        }

        Changed?.Invoke();
    }
}
