using CorporateStarter.Client.Abstractions.Connection;

namespace CorporateStarter.Standalone.Connection;

// The WASM UI runs locally and has no server circuit to reconnect.
// This says nothing about network/API availability: session and API transports handle it.
public sealed class StandaloneConnectionState : IClientConnectionState
{
    public (bool IsConnected, long Revision) Current => (true, 0);
    public event Action? Changed { add { } remove { } }
}
