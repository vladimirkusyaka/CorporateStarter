using System.Net.Http;
using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Auth;

namespace CorporateStarter.Client.Core.Api;

public sealed partial class ClientApiClient
{
    private readonly IClientApiTransport _transport;
    private readonly IClientAuthState _authState;
    private readonly ClientSessionCoordinator _session;

    public ClientApiClient(
        IClientApiTransport transport,
        IClientAuthState authState,
        ClientSessionCoordinator session)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(authState);
        ArgumentNullException.ThrowIfNull(session);
        _transport = transport;
        _authState = authState;
        _session = session;
    }

    public async Task<ClientApiResponse> GetAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        cancellationToken.ThrowIfCancellationRequested();

        var requestSession = _authState.Current;
        if (requestSession.Status == ClientAuthStatus.Revalidating && requestSession.UserId is not null)
        {
            await _session.RestoreForApiAsync(requestSession, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            requestSession = ReadCurrentSession(requestSession);
        }
        RequireAuthenticated(requestSession);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequireAuthenticated(ReadCurrentSession(requestSession));

            ClientApiResponse? response = null;
            try
            {
                response = await _transport.SendAsync(
                    HttpMethod.Get, relativePath, requestSession.UserId!.Value,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (response is null)
                    throw new ClientApiTransportException("The API transport returned no response.");
            }
            catch (ClientApiSessionException) when (attempt == 0)
            {
                // Local token expiry or a concurrent session operation can be recovered once.
            }

            cancellationToken.ThrowIfCancellationRequested();
            var current = ReadCurrentSession(requestSession);

            if (response is not null && (response.StatusCode != 401 || attempt == 1))
            {
                RequireAuthenticated(current);
                return response;
            }

            await _session.RestoreForApiAsync(requestSession, cancellationToken)
                .ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            RequireAuthenticated(ReadCurrentSession(requestSession));
        }

        throw new InvalidOperationException("The API retry limit was exceeded.");
    }

    private ClientAuthSnapshot ReadCurrentSession(ClientAuthSnapshot requestSession)
    {
        var current = _authState.Current;
        if (current.UserId != requestSession.UserId ||
            current.SessionGeneration != requestSession.SessionGeneration)
        {
            throw new ClientApiSessionException(ClientApiSessionFailure.SessionChanged);
        }
        return current;
    }

    private static void RequireAuthenticated(ClientAuthSnapshot snapshot)
    {
        switch (snapshot.Status)
        {
            case ClientAuthStatus.Authenticated:
                return;
            case ClientAuthStatus.Revalidating:
            case ClientAuthStatus.Initializing:
                throw new ClientApiSessionException(ClientApiSessionFailure.Busy);
            case ClientAuthStatus.Unavailable:
                throw new ClientApiTransportException("Session verification is unavailable.");
            default:
                throw new ClientApiSessionException(ClientApiSessionFailure.AccessTokenRequired);
        }
    }
}
