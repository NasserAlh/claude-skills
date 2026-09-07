using OpenOsk.Core.Keys;

namespace OpenOsk.Core.Modifiers;

/// <summary>
/// Sticky-modifier state machine. Tapping a modifier cycles Released → Latched → Locked → Released
/// (or Released → Latched → Released when locking is disabled). Latched modifiers are consumed by the
/// next non-modifier key; locked ones stay until tapped again.
/// </summary>
public sealed class ModifierController
{
    private readonly Dictionary<ModifierKey, LatchState> _states = new();

    public ModifierController()
    {
        foreach (var key in Enum.GetValues<ModifierKey>())
        {
            _states[key] = LatchState.Released;
        }
    }

    public event EventHandler? Changed;

    /// <summary>Whether a second tap locks a modifier. Off means a second tap releases it.</summary>
    public bool AllowLock { get; set; } = true;

    public LatchState this[ModifierKey key] => _states[key];

    public bool IsActive(ModifierKey key) => _states[key] != LatchState.Released;

    /// <summary>Modifiers that are latched or locked, in a stable injection order.</summary>
    public IReadOnlyList<ModifierKey> Active =>
        Enum.GetValues<ModifierKey>().Where(IsActive).ToArray();

    public bool AnyActive => _states.Values.Any(s => s != LatchState.Released);

    public void Tap(ModifierKey key)
    {
        var next = _states[key] switch
        {
            LatchState.Released => LatchState.Latched,
            LatchState.Latched => AllowLock ? LatchState.Locked : LatchState.Released,
            _ => LatchState.Released,
        };
        Set(key, next);
    }

    public void Set(ModifierKey key, LatchState state)
    {
        if (_states[key] == state)
        {
            return;
        }

        _states[key] = state;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Called after a non-modifier key has been sent. Latched modifiers drop; locked ones stay.</summary>
    public void ConsumeLatched()
    {
        var changed = false;
        foreach (var key in Enum.GetValues<ModifierKey>())
        {
            if (_states[key] == LatchState.Latched)
            {
                _states[key] = LatchState.Released;
                changed = true;
            }
        }

        if (changed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ReleaseAll()
    {
        var changed = false;
        foreach (var key in Enum.GetValues<ModifierKey>())
        {
            if (_states[key] != LatchState.Released)
            {
                _states[key] = LatchState.Released;
                changed = true;
            }
        }

        if (changed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
