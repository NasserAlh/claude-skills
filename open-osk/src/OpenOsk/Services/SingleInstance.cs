namespace OpenOsk.Services;

/// <summary>
/// Keeps one keyboard on screen. A second launch signals the first one to show itself and exits.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\OpenOSK.SingleInstance";
    private const string EventName = @"Local\OpenOSK.ShowRequested";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _showEvent;
    private readonly bool _owned;
    private RegisteredWaitHandle? _wait;

    public SingleInstance()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out _owned);
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
    }

    /// <summary>True when this process is the one that should show the keyboard.</summary>
    public bool IsFirst => _owned;

    /// <summary>Called on a thread-pool thread whenever another instance asks us to appear.</summary>
    public void OnShowRequested(Action callback)
    {
        _wait = ThreadPool.RegisterWaitForSingleObject(_showEvent, (_, _) => callback(), null, -1, executeOnlyOnce: false);
    }

    /// <summary>Asks the running instance to show its window.</summary>
    public void RequestShow() => _showEvent.Set();

    public void Dispose()
    {
        _wait?.Unregister(null);
        if (_owned)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
        _showEvent.Dispose();
    }
}
