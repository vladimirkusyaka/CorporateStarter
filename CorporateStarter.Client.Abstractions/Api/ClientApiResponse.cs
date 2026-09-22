namespace CorporateStarter.Client.Abstractions.Api;

/// <summary>
/// A completed HTTP response from a business API endpoint.
/// </summary>
public sealed class ClientApiResponse
{
    public int StatusCode { get; }
    public string Body { get; }
    public string? ContentType { get; }
    public string? RetryAfter { get; }

    public bool IsSuccessStatusCode =>
        StatusCode is >= 200 and <= 299;

    public ClientApiResponse(
        int statusCode,
        string body,
        string? contentType = null,
        string? retryAfter = null)
    {
        if (statusCode is < 200 or > 599)
        {
            throw new ArgumentOutOfRangeException(
                nameof(statusCode),
                statusCode,
                "A final HTTP status code between 200 and 599 is required.");
        }

        ArgumentNullException.ThrowIfNull(body);

        StatusCode = statusCode;
        Body = body;
        ContentType = contentType;
        RetryAfter = retryAfter;
    }
}

