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
        public async Task<ClientLogoutStatus> LogoutAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!await _gate.WaitAsync(0))
                return ClientLogoutStatus.Busy;

            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                _module ??= await js.InvokeAsync<IJSObjectReference>(
                    "import",
                    ModulePath);

                var reply = await _module.InvokeAsync<LogoutReply>(
                    "logout");

                if (reply is null || reply.UserId is not null)
                    return ClientLogoutStatus.Unavailable;

                return reply.Status switch
                {
                    "signed_out" => ClientLogoutStatus.SignedOut,
                    "unsupported" => ClientLogoutStatus.UnsupportedEnvironment,
                    "busy" => ClientLogoutStatus.Busy,
                    _ => ClientLogoutStatus.Unavailable
                };
            }
            catch (Exception exception) when (
                exception is JSDisconnectedException
                    or JSException
                    or OperationCanceledException
                    or JsonException)
            {
                logger.LogWarning(
                    "Browser logout interop failed: {FailureType}.",
                    exception.GetType().Name);

                return ClientLogoutStatus.Unavailable;
            }
            finally
            {
                _gate.Release();
            }
        }

        private sealed record LogoutReply(
            string? Status,
            Guid? UserId);
    }
}
