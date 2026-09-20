using CorporateStarter.Client.Abstractions.Auth;

namespace CorporateStarter.Client.Core.Auth;

public sealed partial class ClientSessionCoordinator
{
    public void SuspendForReconnect()
    {
        ClientAuthSnapshot previous;

        do
        {
            previous = _state.Current;
        }
        while (!_state.TryTransition(
            previous.Revision,
            ClientAuthStatus.Unavailable,
            previous.UserId));
    }

    public Task<bool> RestoreAfterReconnectAsync(
        CancellationToken cancellationToken = default) =>
        RestoreAsync(true, cancellationToken);
}
