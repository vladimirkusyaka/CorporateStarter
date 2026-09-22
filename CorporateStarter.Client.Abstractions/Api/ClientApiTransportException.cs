namespace CorporateStarter.Client.Abstractions.Api;

/// <summary>
/// The transport could not provide a complete HTTP response.
/// This does not imply that the server did not execute the request.
/// </summary>
public sealed class ClientApiTransportException : Exception
{
    public ClientApiTransportException(string message)
        : base(message)
    {
    }

    public ClientApiTransportException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
