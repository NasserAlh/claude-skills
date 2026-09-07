namespace OpenOsk.Core.Modifiers;

/// <summary>State of a sticky modifier key on the on-screen keyboard.</summary>
public enum LatchState
{
    /// <summary>Not active.</summary>
    Released,

    /// <summary>Active for the next key press only, then released automatically.</summary>
    Latched,

    /// <summary>Active until tapped again.</summary>
    Locked,
}
