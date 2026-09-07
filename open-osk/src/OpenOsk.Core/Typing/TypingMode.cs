namespace OpenOsk.Core.Typing;

/// <summary>How the user activates keys. Matches the three modes of the Windows On-Screen Keyboard.</summary>
public enum TypingMode
{
    /// <summary>Click (or tap) a key to type it.</summary>
    Click,

    /// <summary>Point at a key and hold the pointer there for the dwell time.</summary>
    Hover,

    /// <summary>The keyboard highlights rows, then keys; a single switch selects.</summary>
    Scan,
}

/// <summary>What selects the highlighted item in <see cref="TypingMode.Scan"/>.</summary>
public enum ScanSelectSource
{
    /// <summary>A keyboard key (default: Space), registered as a global hotkey while scanning.</summary>
    KeyboardKey,

    /// <summary>A mouse click anywhere on the keyboard window.</summary>
    MouseClick,

    /// <summary>Either the key or a mouse click.</summary>
    Both,
}
