using CorporateStarter.Client.Abstractions.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CorporateStarter.Client.Browser.Auth
{
    public sealed partial class BrowserLoginTransport
    {
        public async Task<ClientSessionRestoreResult> RestoreAsync(
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            await _gate.WaitAsync(cancellationToken);

            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                _module ??= await js.InvokeAsync<IJSObjectReference>(
                    "import",
                    cancellationToken,
                    ModulePath);

                cancellationToken.ThrowIfCancellationRequested();

                // Cancellation does not undo server-side token rotation.
                var reply = await _module.InvokeAsync<RestoreReply>(
                    "restoreSession");

                cancellationToken.ThrowIfCancellationRequested();

                return MapRestore(reply);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is JSDisconnectedException
                    or JSException
                    or OperationCanceledException
                    or JsonException)
            {
                logger.LogWarning(
                    "Browser session restore interop failed: {FailureType}.",
                    exception.GetType().Name);

                return ClientSessionRestoreResult.Unavailable;
            }
            finally
            {
                _gate.Release();
            }
        }

        private static ClientSessionRestoreResult MapRestore(
            RestoreReply? reply)
        {
            if (reply?.Status == "authenticated")
            {
                return reply.UserId is { } id && id != Guid.Empty
                    ? ClientSessionRestoreResult.Authenticated(id)
                    : ClientSessionRestoreResult.Unavailable;
            }

            if (reply?.UserId is not null)
                return ClientSessionRestoreResult.Unavailable;

            return reply?.Status switch
            {
                "anonymous" => ClientSessionRestoreResult.Anonymous,

                "unsupported" =>
                    ClientSessionRestoreResult.UnsupportedEnvironment,

                _ => ClientSessionRestoreResult.Unavailable
            };
        }

        private sealed record RestoreReply(
            string? Status,
            Guid? UserId);
    }
}
