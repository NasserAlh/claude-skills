using OpenOsk.Core.Keys;
using OpenOsk.Core.Modifiers;

namespace OpenOsk.Core.Input;

/// <summary>
/// Turns an activated key plus the current sticky-modifier state into the strokes the OS must see.
/// Pure: no timers, no Win32, so the exact byte-for-byte behaviour is unit-tested.
/// </summary>
public static class KeyStrokePlanner
{
    public static KeyStrokePlan Plan(KeyDefinition key, ModifierController modifiers)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(modifiers);

        if (!key.SendsInput)
        {
            return KeyStrokePlan.Empty;
        }

        var vk = key.VirtualKey;
        var extended = key.Extended;
        if (modifiers.IsActive(ModifierKey.Fn) && key.FnVirtualKey != VirtualKey.None)
        {
            vk = key.FnVirtualKey;
            extended = VirtualKeyInfo.IsExtended(vk);
        }

        if (vk == VirtualKey.None)
        {
            return KeyStrokePlan.Empty;
        }

        var press = new List<KeyStroke>();
        var release = new List<KeyStroke>();
        var held = new List<VirtualKey>();

        foreach (var modifier in modifiers.Active)
        {
            var modifierVk = ModifierKeyInfo.ToVirtualKey(modifier);
            if (modifierVk == VirtualKey.None)
            {
                continue;
            }

            held.Add(modifierVk);
            press.Add(KeyStroke.Down(modifierVk, VirtualKeyInfo.IsExtended(modifierVk)));
        }

        press.Add(KeyStroke.Down(vk, extended));
        release.Add(KeyStroke.Up(vk, extended));
        for (var i = held.Count - 1; i >= 0; i--)
        {
            release.Add(KeyStroke.Up(held[i], VirtualKeyInfo.IsExtended(held[i])));
        }

        // A physical keyboard auto-repeats by sending additional key-down events; do the same.
        var repeat = key.Repeat ? new[] { KeyStroke.Down(vk, extended) } : [];

        return new KeyStrokePlan(press, release, repeat, ConsumesLatchedModifiers: true);
    }
}
