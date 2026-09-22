namespace CorporateStarter.Client.Abstractions.Connection;

/// <summary>
/// Connection required by the UI host (a circuit for Blazor Server).
/// API availability and authentication are separate concerns handled by the session coordinator.
/// </summary>
public interface IClientConnectionState
{
    (bool IsConnected, long Revision) Current { get; }
    event Action? Changed;
}

