namespace CorporateStarter.Client.Abstractions.Api;

/// <summary>
/// The browser could not send the request under the expected session,
/// or discarded its response because the session changed.
/// </summary>
public sealed class ClientApiSessionException : Exception
{
    public ClientApiSessionFailure Failure { get; }

    public ClientApiSessionException(ClientApiSessionFailure failure)
        : base(GetMessage(failure))
    {
        Failure = failure;
    }

    private static string GetMessage(ClientApiSessionFailure failure) =>
        failure switch
        {
            ClientApiSessionFailure.AccessTokenRequired =>
                "A usable browser access token is required.",
            ClientApiSessionFailure.SessionChanged =>
                "The browser session changed during the API operation.",
            ClientApiSessionFailure.Busy =>
                "A browser session operation is in progress.",
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
}