using Microsoft.JSInterop;

namespace CorporateStarter.Client.Browser.Auth;

public sealed class BrowserSessionMonitor(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath =
        "./_content/CorporateStarter.Client.Browser/auth/session-channel.js";

    private readonly SemaphoreSlim _gate = new(1, 1);

    private IJSObjectReference? _module;
    private DotNetObjectReference<BrowserSessionMonitor>? _reference;
    private Func<Task>? _onChanged;
    private long _lease;
    private bool _disposed;

    public async Task StartAsync(Func<Task> onChanged)
    {
        ArgumentNullException.ThrowIfNull(onChanged);

        await _gate.WaitAsync();

        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_lease != 0)
                throw new InvalidOperationException(
                    "Session monitor is already started.");

            _module ??= await js.InvokeAsync<IJSObjectReference>(
                "import", ModulePath);

            _reference ??= DotNetObjectReference.Create(this);
            _onChanged = onChanged;

            _lease = await _module.InvokeAsync<long>(
                "startSessionListener", _reference);
        }
        finally
        {
            _gate.Release();
        }
    }

    [JSInvokable]
    public Task OnSessionChanged() =>
        _disposed
            ? Task.CompletedTask
            : _onChanged?.Invoke() ?? Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();

        try
        {
            if (_disposed)
                return;

            _disposed = true;
            _onChanged = null;

            try
            {
                if (_module is not null)
                {
                    try
                    {
                        if (_lease != 0)
                        {
                            await _module.InvokeVoidAsync(
                                "stopSessionListener", _lease);
                        }
                    }
                    finally
                    {
                        await _module.DisposeAsync();
                    }
                }
            }
            catch (Exception ex) when (
                ex is JSDisconnectedException
                    or JSException
                    or OperationCanceledException)
            {
                // Browser or circuit is no longer available.
            }
            finally
            {
                _reference?.Dispose();
                _reference = null;
                _module = null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}
