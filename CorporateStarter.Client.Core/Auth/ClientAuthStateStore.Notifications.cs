using CorporateStarter.Client.Abstractions.Auth;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Core.Auth
{
    public sealed partial class ClientAuthStateStore
    {
        private readonly Queue<(
            ClientAuthStateChangedEventArgs Change,
            EventHandler<ClientAuthStateChangedEventArgs>? Handlers)> _notifications = new();

        private bool _publishing;

        private void DrainNotifications()
        {
            while (true)
            {
                ClientAuthStateChangedEventArgs change;
                EventHandler<ClientAuthStateChangedEventArgs>? handlers;

                lock (_gate)
                {
                    if (_notifications.Count == 0)
                    {
                        _publishing = false;
                        return;
                    }

                    (change, handlers) = _notifications.Dequeue();
                }

                if (handlers is null)
                    continue;

                foreach (EventHandler<ClientAuthStateChangedEventArgs> handler
                         in handlers.GetInvocationList())
                {
                    try
                    {
                        handler(this, change);
                    }
                    catch (Exception exception)
                    {
                        // A failing subscriber must not suppress other subscribers.
                        try
                        {
                            _logger.LogError(exception,
                                "Auth state subscriber failed at revision {Revision}.",
                                change.Snapshot.Revision);
                        }
                        catch
                        {
                            // A logging provider failure must not stop the queue.
                        }
                    }
                }
            }
        }
    }
}
