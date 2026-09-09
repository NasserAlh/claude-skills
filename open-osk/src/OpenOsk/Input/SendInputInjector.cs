using OpenOsk.Core.Input;
using OpenOsk.Core.Keys;
using OpenOsk.Native;

namespace OpenOsk.Input;

/// <summary>
/// Delivers key strokes with <c>SendInput</c>, the same API the Windows OSK uses. Each batch is one
/// call so modifier and key events cannot interleave with real keyboard input.
/// </summary>
internal sealed class SendInputInjector : IKeyInjector
{
    private readonly Func<IntPtr> _layoutProvider;

    public SendInputInjector(Func<IntPtr> layoutProvider)
    {
        _layoutProvider = layoutProvider;
    }

    public void Send(IReadOnlyList<KeyStroke> strokes)
    {
        if (strokes.Count == 0)
        {
            return;
        }

        var hkl = _layoutProvider();
        var inputs = new NativeMethods.INPUT[strokes.Count];
        for (var i = 0; i < strokes.Count; i++)
        {
            var stroke = strokes[i];
            var vk = ResolveVirtualKey(stroke.Key);
            var scan = (ushort)(NativeMethods.MapVirtualKeyEx((uint)vk, NativeMethods.MAPVK_VK_TO_VSC, hkl) & 0xFF);
            var flags = 0u;
            if (stroke.Extended)
            {
                flags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;
            }

            if (!stroke.IsDown)
            {
                flags |= NativeMethods.KEYEVENTF_KEYUP;
            }

            inputs[i] = Keyboard((ushort)vk, scan, flags);
        }

        Dispatch(inputs);
    }

    public void SendText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var inputs = new NativeMethods.INPUT[text.Length * 2];
        for (var i = 0; i < text.Length; i++)
        {
            inputs[i * 2] = Keyboard(0, text[i], NativeMethods.KEYEVENTF_UNICODE);
            inputs[(i * 2) + 1] = Keyboard(0, text[i], NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP);
        }

        Dispatch(inputs);
    }

    /// <summary>Generic Shift/Ctrl/Alt map to their left-hand keys; SendInput wants a specific side.</summary>
    private static VirtualKey ResolveVirtualKey(VirtualKey key) => key switch
    {
        VirtualKey.Shift => VirtualKey.LShift,
        VirtualKey.Control => VirtualKey.LControl,
        VirtualKey.Menu => VirtualKey.LMenu,
        _ => key,
    };

    private static NativeMethods.INPUT Keyboard(ushort vk, ushort scan, uint flags) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        u = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = vk,
                wScan = scan,
                dwFlags = flags,
                time = 0,
                dwExtraInfo = NativeMethods.InjectionMarker,
            },
        },
    };

    private static void Dispatch(NativeMethods.INPUT[] inputs)
    {
        var size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>();
        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, size);
        if (sent != inputs.Length)
        {
            // Typically means the foreground window is elevated and we are not. Nothing to do but
            // move on; the user sees nothing typed, exactly like the Windows OSK in this situation.
            System.Diagnostics.Debug.WriteLine($"SendInput sent {sent}/{inputs.Length} events (error {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}).");
        }
    }
}
