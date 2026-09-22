using System.Net.Http;
using System.Text;
using System.Text.Json;
using CorporateStarter.Client.Abstractions.Api;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace CorporateStarter.Client.Browser.Api;

public sealed class BrowserApiTransport(
    IJSRuntime js,
    ILogger<BrowserApiTransport> logger) : IClientApiTransport
{
    private const string ModulePath =
        "./_content/CorporateStarter.Client.Browser/auth/browser-session.js";

    private const int RequestTimeoutMilliseconds = 30_000;
    private const long MaxResponseBytes = 4 * 1024 * 1024;
    private static readonly TimeSpan InteropTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(2);

    public async Task<ClientApiResponse> SendAsync(
        HttpMethod method,
        string relativePath,
        Guid expectedUserId,
        string? jsonBody = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (expectedUserId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(expectedUserId));

        var verb = method.Method.ToUpperInvariant();
        if (verb is not ("GET" or "POST" or "PUT" or "DELETE"))
            throw new ArgumentException("Unsupported API method.", nameof(method));
        if (verb == "GET" && jsonBody is not null)
            throw new ArgumentException("GET requests cannot have a body.", nameof(jsonBody));

        cancellationToken.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(InteropTimeout);

        var requestId = Guid.NewGuid().ToString("D");
        IJSObjectReference? module = null;
        IJSStreamReference? responseStream = null;
        var sendStarted = false;
        var sendCompleted = false;

        try
        {
            module = await js.InvokeAsync<IJSObjectReference>(
                "import", deadline.Token, ModulePath);

            deadline.Token.ThrowIfCancellationRequested();
            sendStarted = true;
            var reply = await module.InvokeAsync<ApiReply>(
                "sendApiRequest", deadline.Token, new
                {
                    id = requestId,
                    method = verb,
                    relativePath,
                    expectedUserId = expectedUserId.ToString("D"),
                    jsonBody,
                    timeoutMilliseconds = RequestTimeoutMilliseconds
                });

            sendCompleted = true;
            responseStream = reply?.BodyStream;
            cancellationToken.ThrowIfCancellationRequested();
            return await MapReplyAsync(reply, deadline.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (OperationCanceledException exception)
        {
            throw new ClientApiTransportException(
                "The browser API operation did not complete in time.", exception);
        }
        catch (Exception exception) when (
            exception is JSDisconnectedException or JSException or JsonException or IOException or DecoderFallbackException)
        {
            logger.LogWarning(
                "Browser API interop failed: {FailureType}.", exception.GetType().Name);
            throw new ClientApiTransportException(
                "The browser could not provide a complete API response.", exception);
        }
        finally
        {
            if (responseStream is not null)
                await TryDisposeAsync(responseStream);

            if (module is not null)
            {
                if (sendStarted && !sendCompleted)
                    await TryCancelAsync(module, requestId);

                await TryDisposeAsync(module);
            }
        }
    }

    private static async Task<ClientApiResponse> MapReplyAsync(
        ApiReply? reply, CancellationToken cancellationToken)
    {
        switch (reply?.Kind)
        {
            case "response":
                if (reply.StatusCode is not (>= 200 and <= 599) ||
                    (reply.Body is null) == (reply.BodyStream is null))
                    throw new ClientApiTransportException("The browser returned an invalid HTTP response.");
                var body = reply.Body;
                if (reply.BodyStream is not null)
                {
                    if (reply.BodyStream.Length is < 0 or > MaxResponseBytes)
                        throw new ClientApiTransportException("The API response exceeded the size limit.");
                    await using var stream = await reply.BodyStream.OpenReadStreamAsync(
                        MaxResponseBytes, cancellationToken);
                    using var reader = new StreamReader(
                        stream, new UTF8Encoding(false, true),
                        detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
                    body = await reader.ReadToEndAsync(cancellationToken);
                }
                return new ClientApiResponse(
                    reply.StatusCode.Value, body!, reply.ContentType, reply.RetryAfter);

            case "session_required":
                throw new ClientApiSessionException(ClientApiSessionFailure.AccessTokenRequired);
            case "session_changed":
                throw new ClientApiSessionException(ClientApiSessionFailure.SessionChanged);
            case "busy":
                throw new ClientApiSessionException(ClientApiSessionFailure.Busy);
            case "invalid_request":
                throw new ArgumentException("The browser rejected the API request parameters.");
            case "timeout":
                throw new ClientApiTransportException("The API request timed out.");
            case "cancelled":
                throw new ClientApiTransportException("The browser aborted the API request.");
            case "unavailable":
                throw new ClientApiTransportException("A complete API response is unavailable.");
            default:
                throw new ClientApiTransportException("The browser returned an unknown API result.");
        }
    }

    private async Task TryCancelAsync(IJSObjectReference module, string requestId)
    {
        using var deadline = new CancellationTokenSource(CleanupTimeout);
        try
        {
            await module.InvokeVoidAsync("cancelApiRequest", deadline.Token, requestId);
        }
        catch (Exception exception) when (
            exception is JSDisconnectedException or JSException or OperationCanceledException or ObjectDisposedException)
        {
            logger.LogDebug(
                "Browser API cancellation could not be delivered: {FailureType}.",
                exception.GetType().Name);
        }
    }

    private async Task TryDisposeAsync(IAsyncDisposable reference)
    {
        try
        {
            await reference.DisposeAsync();
        }
        catch (Exception exception) when (
            exception is JSDisconnectedException or JSException or OperationCanceledException or ObjectDisposedException)
        {
            logger.LogDebug(
                "Browser API reference disposal failed: {FailureType}.",
                exception.GetType().Name);
        }
    }

    private sealed record ApiReply(
        string? Kind,
        int? StatusCode,
        string? Body,
        string? ContentType,
        string? RetryAfter,
        IJSStreamReference? BodyStream);
}
