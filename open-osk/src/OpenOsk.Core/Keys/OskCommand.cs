namespace OpenOsk.Core.Keys;

/// <summary>Commands that act on the keyboard window itself. Mirrors the Windows OSK command column.</summary>
public enum OskCommand
{
    None,

    /// <summary>Switch to the compact navigation layout ("Nav").</summary>
    Navigation,

    /// <summary>Return from the navigation layout to the full keyboard ("Gen").</summary>
    General,

    /// <summary>Move the keyboard to the top of the screen ("Mv Up").</summary>
    MoveUp,

    /// <summary>Move the keyboard to the bottom of the screen ("Mv Dn").</summary>
    MoveDown,

    /// <summary>Dock the keyboard to the bottom edge, reserving the space ("Dock").</summary>
    Dock,

    /// <summary>Toggle the translucent state ("Fade").</summary>
    Fade,

    /// <summary>Open the options dialog.</summary>
    Options,

    /// <summary>Open the help page.</summary>
    Help,

    /// <summary>Show or hide the numeric key pad.</summary>
    NumPad,
}
