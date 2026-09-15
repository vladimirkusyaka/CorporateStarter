using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Client.Browser.auth
{
    public sealed class BrowserCapabilityProbe : IAsyncDisposable
    {
        private const string ModulePath =
            "./_content/CorporateStarter.Client.Browser/auth/browser-capabilities.js";

        private readonly IJSRuntime _js;
        private readonly SemaphoreSlim _gate = new(1, 1);

        private IJSObjectReference? _module;
        private bool _disposed;

        public BrowserCapabilityProbe(IJSRuntime js)
        {
            ArgumentNullException.ThrowIfNull(js);
            _js = js;
        }

        public async Task<BrowserCapabilities> CheckAsync(
            CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                _module ??= await _js.InvokeAsync<IJSObjectReference>(
                    "import",
                    cancellationToken,
                    ModulePath).ConfigureAwait(false);

                var result = await _module.InvokeAsync<BrowserCapabilities>(
                    "inspectCapabilities",
                    cancellationToken).ConfigureAwait(false);

                return result ?? throw new JSException(
                    "Browser capability module returned no result.");
            }
            finally
            {
                _gate.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);

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
                        await module.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (JSDisconnectedException)
                    {
                        // Circuit закрыт: освобождение JS-ссылки недоступно.
                    }
                }
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
