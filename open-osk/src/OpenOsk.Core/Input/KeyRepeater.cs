namespace OpenOsk.Core.Input;

/// <summary>
/// Auto-repeat schedule for a held key. A physical keyboard repeats at a fixed rate, but a UI timer
/// fires late and unevenly (a 33 ms WPF timer ticks about every 47 ms), so instead of sending one
/// repeat per tick the host asks how many repeats are owed since the last tick and sends that many.
/// Time is injected so the arithmetic is testable.
/// </summary>
public sealed class KeyRepeater
{
    /// <summary>Most repeats handed out in one tick. A stalled UI thread must not flood the target.</summary>
    public const int MaxBurst = 3;

    private TimeSpan _next;
    private bool _running;

    public KeyRepeater(TimeSpan delay, TimeSpan interval)
    {
        Delay = delay;
        Interval = interval;
    }

    /// <summary>Time from the key going down to the first repeat.</summary>
    public TimeSpan Delay { get; set; }

    /// <summary>Time between repeats.</summary>
    public TimeSpan Interval { get; set; }

    public bool IsRunning => _running;

    /// <summary>The key went down at <paramref name="now"/>; the first repeat is due after <see cref="Delay"/>.</summary>
    public void Start(TimeSpan now)
    {
        _next = now + Delay;
        _running = true;
    }

    public void Stop() => _running = false;

    /// <summary>
    /// Number of repeats due at <paramref name="now"/>, at most <see cref="MaxBurst"/>. Late ticks are
    /// caught up so the average rate matches <see cref="Interval"/>; after a long stall the backlog is
    /// dropped instead, as a real keyboard's would be.
    /// </summary>
    public int Due(TimeSpan now)
    {
        if (!_running || now < _next)
        {
            return 0;
        }

        var interval = Interval > TimeSpan.Zero ? Interval : TimeSpan.FromMilliseconds(1);
        var owed = 1 + (int)((now - _next).Ticks / interval.Ticks);
        if (owed > MaxBurst)
        {
            _next = now + interval;
            return MaxBurst;
        }

        _next += TimeSpan.FromTicks(interval.Ticks * owed);
        return owed;
    }
}
