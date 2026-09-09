namespace OpenOsk.Core.Keys;

/// <summary>
/// One key in a layout. Immutable; the view layers bind to it and the planner turns it into key strokes.
/// </summary>
public sealed record KeyDefinition
{
    public required string Id { get; init; }

    public required KeyKind Kind { get; init; }

    public VirtualKey VirtualKey { get; init; } = VirtualKey.None;

    /// <summary>Virtual key to send instead of <see cref="VirtualKey"/> while Fn is active.</summary>
    public VirtualKey FnVirtualKey { get; init; } = VirtualKey.None;

    /// <summary>Fixed label. For <see cref="KeyKind.Character"/> keys this is only the fallback.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Optional secondary label shown in the corner (e.g. the shifted symbol).</summary>
    public string? ShiftLabel { get; init; }

    /// <summary>Width in key units. A standard letter key is 1.0.</summary>
    public double Width { get; init; } = 1.0;

    /// <summary>Inject with the extended-key flag (0xE0 scan-code prefix).</summary>
    public bool Extended { get; init; }

    public ModifierKey? Modifier { get; init; }

    public OskCommand Command { get; init; } = OskCommand.None;

    /// <summary>Whether holding the key repeats it, like a physical keyboard.</summary>
    public bool Repeat { get; init; } = true;

    public bool IsGap => Kind == KeyKind.Gap;

    public bool SendsInput => Kind is KeyKind.Character or KeyKind.Action or KeyKind.Lock;

    public static KeyDefinition Gap(double width) => new()
    {
        Id = $"gap-{Guid.NewGuid():N}",
        Kind = KeyKind.Gap,
        Width = width,
        Repeat = false,
    };
}
