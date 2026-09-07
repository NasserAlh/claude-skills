namespace OpenOsk.Core.Keys;

/// <summary>Modifier keys that can be latched or locked on the on-screen keyboard.</summary>
public enum ModifierKey
{
    Shift,
    Control,
    Alt,
    Win,

    /// <summary>Fn is internal: it remaps the number row to F1..F12 and is never sent to the OS.</summary>
    Fn,
}

public static class ModifierKeyInfo
{
    /// <summary>The virtual key injected for a modifier, or <see cref="VirtualKey.None"/> for Fn.</summary>
    public static VirtualKey ToVirtualKey(ModifierKey modifier) => modifier switch
    {
        ModifierKey.Shift => VirtualKey.LShift,
        ModifierKey.Control => VirtualKey.LControl,
        ModifierKey.Alt => VirtualKey.LMenu,
        ModifierKey.Win => VirtualKey.LWin,
        _ => VirtualKey.None,
    };
}
