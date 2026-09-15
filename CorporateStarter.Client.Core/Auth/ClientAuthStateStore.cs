using CorporateStarter.Client.Abstractions.Auth;
using Microsoft.Extensions.Logging;

namespace CorporateStarter.Client.Core.Auth;

public sealed partial class ClientAuthStateStore : IClientAuthState
{
    private readonly object _gate = new();
    private readonly ILogger<ClientAuthStateStore> _logger;
    private ClientAuthSnapshot _current = ClientAuthSnapshot.Initial;
    private EventHandler<ClientAuthStateChangedEventArgs>? _handlers;

    public ClientAuthStateStore(ILogger<ClientAuthStateStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public ClientAuthSnapshot Current
    {
        get { lock (_gate) return _current; }
    }

    public event EventHandler<ClientAuthStateChangedEventArgs>? StateChanged
    {
        add { lock (_gate) _handlers += value; }
        remove { lock (_gate) _handlers -= value; }
    }

    public bool TryTransition(
        long expectedRevision,
        ClientAuthStatus status,
        Guid? userId = null,
        ClientSessionInvalidationReason? invalidationReason = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(expectedRevision);
        bool drain;

        lock (_gate)
        {
            if (_current.Revision != expectedRevision)
                return false;

            ValidateTransition(_current, status, userId, invalidationReason);

            var generationChanged = invalidationReason is not null
                || _current.UserId != userId;

            var next = new ClientAuthSnapshot(
                status,
                userId,
                checked(_current.Revision + 1),
                generationChanged
                    ? checked(_current.SessionGeneration + 1)
                    : _current.SessionGeneration);

            var change = new ClientAuthStateChangedEventArgs(
                next, invalidationReason);

            _current = next;
            _notifications.Enqueue((change, _handlers));
            drain = !_publishing;
            _publishing = true;
        }

        if (drain)
            DrainNotifications();

        return true;
    }
}
