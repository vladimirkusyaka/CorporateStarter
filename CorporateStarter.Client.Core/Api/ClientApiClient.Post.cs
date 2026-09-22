using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;

namespace CorporateStarter.Client.Core.Api;

public sealed partial class ClientApiClient
{
    public Task<TResponse> PostJsonAsync<TRequest, TResponse>(
        string relativePath, TRequest request, JsonTypeInfo<TRequest> requestType,
        JsonTypeInfo<TResponse> responseType, CancellationToken cancellationToken = default) =>
        SendWriteJsonAsync(HttpMethod.Post, relativePath, request, requestType, responseType, cancellationToken);

    public Task<TResponse> PutJsonAsync<TRequest, TResponse>(
        string relativePath, TRequest request, JsonTypeInfo<TRequest> requestType,
        JsonTypeInfo<TResponse> responseType, CancellationToken cancellationToken = default) =>
        SendWriteJsonAsync(HttpMethod.Put, relativePath, request, requestType, responseType, cancellationToken);

    private async Task<TResponse> SendWriteJsonAsync<TRequest, TResponse>(
        HttpMethod method, string relativePath, TRequest request, JsonTypeInfo<TRequest> requestType,
        JsonTypeInfo<TResponse> responseType, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(responseType);
        cancellationToken.ThrowIfCancellationRequested();
        var body = JsonSerializer.Serialize(request, requestType);
        var initial = _authState.Current;
        var response = await SendWriteAsync(method, relativePath, body, initial, cancellationToken)
            .ConfigureAwait(false);
        return ReadJson(response, responseType, initial, cancellationToken);
    }

    public async Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var response = await SendWriteAsync(HttpMethod.Delete, relativePath, null,
            _authState.Current, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new ClientApiHttpException(response.StatusCode, response.RetryAfter);
        if (response.StatusCode != 204)
            throw new InvalidDataException("The DELETE endpoint did not confirm completion with HTTP 204.");
    }

    private async Task<ClientApiResponse> SendWriteAsync(HttpMethod method, string relativePath,
        string? body, ClientAuthSnapshot initial, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        cancellationToken.ThrowIfCancellationRequested();
        if (initial.UserId is null || initial.Status is not
            (ClientAuthStatus.Authenticated or ClientAuthStatus.Revalidating))
            RequireAuthenticated(initial);

        // Validate the session before a write; never replay an uncertain write.
        await _session.RestoreForApiAsync(initial, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        RequireAuthenticated(ReadCurrentSession(initial));

        var response = await _transport.SendAsync(method, relativePath,
            initial.UserId!.Value, body, cancellationToken).ConfigureAwait(false);
        if (response is null)
            throw new ClientApiTransportException("The API transport returned no response.");
        cancellationToken.ThrowIfCancellationRequested();
        RequireAuthenticated(ReadCurrentSession(initial));
        return response;
    }
}
