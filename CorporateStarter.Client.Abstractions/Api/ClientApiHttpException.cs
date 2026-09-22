namespace CorporateStarter.Client.Abstractions.Api;

/// <summary>
/// The API returned an unsuccessful HTTP status.
/// The raw response body is deliberately not included in the exception message.
/// </summary>
public sealed class ClientApiHttpException : Exception
{
    public int StatusCode { get; }
    public string? RetryAfter { get; }

    public ClientApiHttpException(int statusCode, string? retryAfter = null)
        : base($"The API returned HTTP {statusCode}.")
    {
        if (statusCode is < 300 or > 599)
            throw new ArgumentOutOfRangeException(nameof(statusCode));

        StatusCode = statusCode;
        RetryAfter = retryAfter;
    }
}

