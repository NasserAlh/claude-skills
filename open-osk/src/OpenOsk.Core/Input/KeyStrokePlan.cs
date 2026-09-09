namespace OpenOsk.Core.Input;

/// <summary>
/// The strokes to send when a key goes down and when it comes back up. Splitting the two lets the
/// host hold a key for auto-repeat while modifiers stay pressed around it.
/// </summary>
public sealed record KeyStrokePlan(
    IReadOnlyList<KeyStroke> Press,
    IReadOnlyList<KeyStroke> Release,
    IReadOnlyList<KeyStroke> Repeat,
    bool ConsumesLatchedModifiers)
{
    public static KeyStrokePlan Empty { get; } = new([], [], [], false);

    /// <summary>All strokes of a complete tap, press followed by release.</summary>
    public IReadOnlyList<KeyStroke> Tap => [.. Press, .. Release];
}
