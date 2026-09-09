using System.Windows.Interop;
using OpenOsk.Core.Keys;
using OpenOsk.Native;

namespace OpenOsk.Input;

/// <summary>
/// A system-wide key registered with <c>RegisterHotKey</c>. Used as the switch in scan mode. While
/// registered the key is consumed by OpenOSK, exactly as the Windows OSK does with its scan key.
/// </summary>
internal sealed class GlobalHotKey : IDisposable
{
    private const int Id = 0x0501;
    private readonly HwndSource _source;
    private bool _registered;

    public GlobalHotKey(HwndSource source)
    {
        _source = source;
        _source.AddHook(WndProc);
    }

    public event EventHandler? Pressed;

    public VirtualKey Key { get; private set; } = VirtualKey.None;

    public bool IsRegistered => _registered;

    public bool Register(VirtualKey key)
    {
        Unregister();
        if (key == VirtualKey.None)
        {
            return false;
        }

        _registered = NativeMethods.RegisterHotKey(_source.Handle, Id, NativeMethods.MOD_NOREPEAT, (uint)key);
        Key = _registered ? key : VirtualKey.None;
        return _registered;
    }

    public void Unregister()
    {
        if (_registered)
        {
            NativeMethods.UnregisterHotKey(_source.Handle, Id);
            _registered = false;
            Key = VirtualKey.None;
        }
    }

    public void Dispose()
    {
        Unregister();
        _source.RemoveHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == Id)
        {
            Pressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }
}
