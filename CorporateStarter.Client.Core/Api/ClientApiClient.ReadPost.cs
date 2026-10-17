using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace CorporateStarter.Client.Core.Api;

public sealed partial class ClientApiClient
{
    /// <summary>
    /// Sends a read-only POST query. Only use for endpoints without side effects:
    /// a session failure or HTTP 401 may refresh the session and replay the request once.
    /// </summary>
    public async Task<TResponse> PostReadJsonAsync<TRequest, TResponse>(
        string relativePath, TRequest request, JsonTypeInfo<TRequest> requestType,
        JsonTypeInfo<TResponse> responseType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(responseType);
        cancellationToken.ThrowIfCancellationRequested();
        var body = JsonSerializer.Serialize(request, requestType);
        var initial = _authState.Current;
        var response = await SendReadAsync(HttpMethod.Post, relativePath, body, cancellationToken)
            .ConfigureAwait(false);
        return ReadJson(response, responseType, initial, cancellationToken);
    }
}
