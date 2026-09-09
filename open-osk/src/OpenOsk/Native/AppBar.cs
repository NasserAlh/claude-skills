using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace OpenOsk.Native;

/// <summary>
/// Docks the keyboard to the bottom screen edge as a shell app bar, so maximised windows shrink to
/// make room instead of being covered. This is what the Windows OSK "Dock" button does.
/// </summary>
internal sealed class AppBar : IDisposable
{
    private const uint ABM_NEW = 0;
    private const uint ABM_REMOVE = 1;
    private const uint ABM_QUERYPOS = 2;
    private const uint ABM_SETPOS = 3;
    private const uint ABE_BOTTOM = 3;
    private const int ABN_POSCHANGED = 1;

    private readonly Window _window;
    private readonly HwndSource _source;
    private readonly uint _callbackMessage;
    private int _heightPx;
    private bool _registered;

    public AppBar(Window window)
    {
        _window = window;
        _source = (HwndSource)PresentationSource.FromVisual(window)!;
        _callbackMessage = NativeMethods.RegisterWindowMessage("OpenOSK.AppBarCallback");
        _source.AddHook(WndProc);
    }

    public bool IsDocked => _registered;

    public void Dock(int heightPx)
    {
        _heightPx = Math.Max(heightPx, 1);
        if (!_registered)
        {
            var data = NewData();
            NativeMethods.SHAppBarMessage(ABM_NEW, ref data);
            _registered = true;
        }

        Position();
    }

    public void Undock()
    {
        if (!_registered)
        {
            return;
        }

        var data = NewData();
        NativeMethods.SHAppBarMessage(ABM_REMOVE, ref data);
        _registered = false;
    }

    public void Dispose()
    {
        Undock();
        _source.RemoveHook(WndProc);
    }

    private void Position()
    {
        var (monitor, _) = WindowHelper.MonitorRects(_window);
        var data = NewData();
        data.uEdge = ABE_BOTTOM;
        data.rc = new NativeMethods.RECT
        {
            Left = monitor.Left,
            Right = monitor.Right,
            Top = monitor.Bottom - _heightPx,
            Bottom = monitor.Bottom,
        };
        NativeMethods.SHAppBarMessage(ABM_QUERYPOS, ref data);
        data.rc.Top = data.rc.Bottom - _heightPx;
        NativeMethods.SHAppBarMessage(ABM_SETPOS, ref data);
        WindowHelper.MoveTo(_window, data.rc.Left, data.rc.Top, data.rc.Width, data.rc.Height);
    }

    private NativeMethods.APPBARDATA NewData() => new()
    {
        cbSize = (uint)Marshal.SizeOf<NativeMethods.APPBARDATA>(),
        hWnd = _source.Handle,
        uCallbackMessage = _callbackMessage,
    };

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_registered && msg == (int)_callbackMessage && wParam.ToInt32() == ABN_POSCHANGED)
        {
            Position();
            handled = true;
        }

        return IntPtr.Zero;
    }
}
