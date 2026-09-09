using OpenOsk.Core.Keys;

namespace OpenOsk.Core.Input;

/// <summary>A single key-down or key-up event to hand to the OS.</summary>
public readonly record struct KeyStroke(VirtualKey Key, bool IsDown, bool Extended)
{
    public static KeyStroke Down(VirtualKey key, bool extended = false) => new(key, true, extended);

    public static KeyStroke Up(VirtualKey key, bool extended = false) => new(key, false, extended);

    public override string ToString() => $"{Key}{(Extended ? "(ext)" : string.Empty)} {(IsDown ? "down" : "up")}";
}
