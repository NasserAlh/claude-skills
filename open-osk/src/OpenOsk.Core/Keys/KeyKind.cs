namespace OpenOsk.Core.Keys;

/// <summary>What a key on the on-screen keyboard does when it is activated.</summary>
public enum KeyKind
{
    /// <summary>Produces a character. Its label follows the active input language.</summary>
    Character,

    /// <summary>A key with a fixed label that sends a virtual key (Enter, Tab, arrows, F-keys...).</summary>
    Action,

    /// <summary>Shift, Ctrl, Alt, Win or Fn. Latches for the next key press instead of being sent alone.</summary>
    Modifier,

    /// <summary>Caps Lock, Num Lock, Scroll Lock. Sent as a tap; the indicator mirrors the real lock state.</summary>
    Lock,

    /// <summary>An OpenOSK command such as Nav, Mv Up, Dock or Fade. Nothing is sent to the OS.</summary>
    Command,

    /// <summary>Empty space in a row.</summary>
    Gap,
}
