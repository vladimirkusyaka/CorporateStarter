using CorporateStarter.Client.Abstractions.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Text.Json;

namespace CorporateStarter.Client.Browser.Auth
{
    public sealed partial class BrowserLoginTransport(
    IJSRuntime js,
    ILogger<BrowserLoginTransport> logger)
    : IClientLoginTransport,
      IClientSessionTransport,
      IClientLogoutTransport,
      IAsyncDisposable
    {
        private const string ModulePath =
            "./_content/CorporateStarter.Client.Browser/auth/browser-session.js";

        private readonly SemaphoreSlim _gate = new(1, 1);
        private IJSObjectReference? _module;
        private bool _disposed;

        public async Task<ClientLoginResult> LoginAsync(
            string login,
            string password)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!await _gate.WaitAsync(0))
                return new(ClientLoginStatus.Busy);

            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                _module ??= await js.InvokeAsync<IJSObjectReference>(
                    "import",
                    ModulePath);

                var reply = await _module.InvokeAsync<LoginReply>(
                    "login",
                    login,
                    password);

                return Map(reply);
            }
            catch (Exception exception) when (
                exception is JSDisconnectedException
                    or JSException
                    or OperationCanceledException
                    or JsonException)
            {
                logger.LogWarning(
                    "Browser login interop failed: {FailureType}.",
                    exception.GetType().Name);

                return new(ClientLoginStatus.Unavailable);
            }
            finally
            {
                _gate.Release();
            }
        }

        private static ClientLoginResult Map(LoginReply? reply)
        {
            var status = reply?.Status switch
            {
                "authenticated" => ClientLoginStatus.Authenticated,
                "rejected" => ClientLoginStatus.Rejected,
                "rate_limited" => ClientLoginStatus.RateLimited,
                "mfa_required" => ClientLoginStatus.MfaRequired,
                "unsupported" => ClientLoginStatus.UnsupportedEnvironment,
                "invalid_input" => ClientLoginStatus.InvalidInput,
                "busy" => ClientLoginStatus.Busy,
                _ => ClientLoginStatus.Unavailable
            };

            var id = reply?.UserId;

            if (id == Guid.Empty ||
                (status == ClientLoginStatus.Authenticated) !=
                (id is not null))
            {
                return new(ClientLoginStatus.Unavailable);
            }

            return new(status, id);
        }

        public async ValueTask DisposeAsync()
        {
            await _gate.WaitAsync();

            try
            {
                if (_disposed)
                    return;

                _disposed = true;

                var module = _module;
                _module = null;

                if (module is not null)
                {
                    try
                    {
                        await module.DisposeAsync();
                    }
                    catch (Exception exception) when (
                        exception is JSDisconnectedException
                            or JSException
                            or OperationCanceledException)
                    {
                        logger.LogDebug(
                            "Browser login module disposal failed: {FailureType}.",
                            exception.GetType().Name);
                    }
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        private sealed record LoginReply(
            string? Status,
            Guid? UserId);
    }
}
