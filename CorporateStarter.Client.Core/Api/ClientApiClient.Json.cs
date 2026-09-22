using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using CorporateStarter.Client.Abstractions.Api;

namespace CorporateStarter.Client.Core.Api;

public sealed partial class ClientApiClient
{
    public async Task<T> GetJsonAsync<T>(
        string relativePath,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);
        var requestSession = _authState.Current;
        var response = await GetAsync(relativePath, cancellationToken)
            .ConfigureAwait(false);

        return ReadJson(response, jsonTypeInfo, requestSession, cancellationToken);
    }

    private T ReadJson<T>(ClientApiResponse response, JsonTypeInfo<T> jsonTypeInfo,
        CorporateStarter.Client.Abstractions.Auth.ClientAuthSnapshot requestSession,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            throw new ClientApiHttpException(response.StatusCode, response.RetryAfter);

        if (!MediaTypeHeaderValue.TryParse(response.ContentType, out var contentType) ||
            contentType.MediaType is not { } mediaType ||
            !(mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
              (mediaType.StartsWith("application/", StringComparison.OrdinalIgnoreCase) &&
               mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase))))
        {
            throw new InvalidDataException("The API response is not JSON.");
        }

        T value;
        try
        {
            value = JsonSerializer.Deserialize(response.Body, jsonTypeInfo)
                ?? throw new InvalidDataException("The API returned a null JSON value.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "The API response does not match the expected JSON format.", exception);
        }

        cancellationToken.ThrowIfCancellationRequested();
        RequireAuthenticated(ReadCurrentSession(requestSession));
        return value;
    }
}
