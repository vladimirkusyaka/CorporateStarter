using Microsoft.JSInterop;

namespace CorporateStarter.Client.Browser.Auth;

public sealed class BrowserIdleMonitor(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<BrowserIdleMonitor>? _reference;
    private Func<Task>? _revalidate;
    private bool _disposed;
    private long _lease;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task StartAsync(Func<Task> revalidate)
    {
        await _gate.WaitAsync();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _revalidate = revalidate;
            _module ??= await js.InvokeAsync<IJSObjectReference>("import",
                "./_content/CorporateStarter.Client.Browser/auth/browser-session.js");
            _reference ??= DotNetObjectReference.Create(this);
            _lease = await _module.InvokeAsync<long>("startIdleMonitor", _reference);
        }
        finally { _gate.Release(); }
    }

    [JSInvokable]
    public Task RevalidateIdleSession() =>
        _disposed ? Task.CompletedTask : _revalidate?.Invoke() ?? Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_disposed) return;
            _disposed = true;
            _revalidate = null;
            try
            {
                if (_module is not null)
                {
                    await _module.InvokeVoidAsync("stopIdleMonitor", _lease);
                    await _module.DisposeAsync();
                }
            }
            catch (Exception ex) when (ex is JSDisconnectedException or JSException or OperationCanceledException)
            {
                // Browser or circuit is already gone.
            }
            finally { _reference?.Dispose(); }
        }
        finally { _gate.Release(); }
    }
}
