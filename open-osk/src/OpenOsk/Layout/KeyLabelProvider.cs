using System.Text;
using OpenOsk.Core.Keys;
using OpenOsk.Native;

namespace OpenOsk.Layout;

/// <summary>
/// Asks Windows what a virtual key produces under the active input language, so labels follow the
/// user's keyboard layout (AZERTY, QWERTZ, Arabic...) without OpenOSK shipping per-language tables.
/// </summary>
internal sealed class KeyLabelProvider
{
    // Bit 2: do not change the keyboard's dead-key state while translating (Windows 10 1607+).
    private const uint DoNotChangeKeyboardState = 1u << 2;

    private readonly Dictionary<(VirtualKey, bool, bool, long), string?> _cache = new();

    public void Invalidate() => _cache.Clear();

    /// <summary>The character for <paramref name="key"/>, or null if the layout has none.</summary>
    public string? Character(VirtualKey key, bool shift, bool capsLock, IntPtr layout)
    {
        var cacheKey = (key, shift, capsLock, layout.ToInt64());
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var state = new byte[256];
        if (shift)
        {
            state[(int)VirtualKey.Shift] = 0x80;
            state[(int)VirtualKey.LShift] = 0x80;
        }

        if (capsLock)
        {
            state[(int)VirtualKey.Capital] = 0x01;
        }

        var scan = NativeMethods.MapVirtualKeyEx((uint)key, NativeMethods.MAPVK_VK_TO_VSC, layout);
        var buffer = new StringBuilder(8);
        var result = NativeMethods.ToUnicodeEx((uint)key, scan, state, buffer, buffer.Capacity, DoNotChangeKeyboardState, layout);
        string? text = result switch
        {
            > 0 => buffer.ToString(0, result),
            < 0 => buffer.Length > 0 ? buffer.ToString(0, 1) : null, // dead key: show the diacritic
            _ => null,
        };

        if (text is not null && (text.Length == 0 || char.IsControl(text[0]) || text[0] == ' '))
        {
            text = null;
        }

        _cache[cacheKey] = text;
        return text;
    }
}
