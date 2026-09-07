namespace OpenOsk.Core.Typing;

/// <summary>
/// Hover-to-type timing. The host calls <see cref="Enter"/> and <see cref="Leave"/> as the pointer
/// moves and <see cref="Tick"/> on a timer; the tracker decides when the dwell completes. Time is
/// injected so the logic is testable.
/// </summary>
public sealed class DwellTracker
{
    private string? _target;
    private TimeSpan _enteredAt;
    private bool _fired;

    public DwellTracker(TimeSpan dwellTime)
    {
        DwellTime = dwellTime;
    }

    public TimeSpan DwellTime { get; set; }

    /// <summary>Id of the key currently under the pointer, or null.</summary>
    public string? Target => _target;

    public void Enter(string keyId, TimeSpan now)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyId);
        if (_target == keyId)
        {
            return;
        }

        _target = keyId;
        _enteredAt = now;
        _fired = false;
    }

    public void Leave()
    {
        _target = null;
        _fired = false;
    }

    /// <summary>Fraction of the dwell time elapsed for the current target, clamped to 0..1.</summary>
    public double Progress(TimeSpan now)
    {
        if (_target is null || DwellTime <= TimeSpan.Zero)
        {
            return 0;
        }

        var elapsed = now - _enteredAt;
        return Math.Clamp(elapsed / DwellTime, 0, 1);
    }

    /// <summary>
    /// Returns the key id exactly once when its dwell completes, otherwise null. After firing, the
    /// pointer must leave and re-enter a key before another activation can happen.
    /// </summary>
    public string? Tick(TimeSpan now)
    {
        if (_target is null || _fired)
        {
            return null;
        }

        if (now - _enteredAt < DwellTime)
        {
            return null;
        }

        _fired = true;
        return _target;
    }
}
