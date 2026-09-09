using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace OpenOsk.Native;

/// <summary>Window-style tweaks that WPF does not expose directly.</summary>
internal static class WindowHelper
{
    public static IntPtr Handle(Window window) => new WindowInteropHelper(window).Handle;

    /// <summary>
    /// Makes the window never take keyboard focus. Clicking it leaves the target application active,
    /// which is what lets injected keys land in that application.
    /// </summary>
    public static void MakeNonActivating(Window window)
    {
        var hwnd = Handle(window);
        var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
        style |= NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOPMOST | NativeMethods.WS_EX_APPWINDOW;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(style));
    }

    /// <summary>Device-pixels-per-DIP scale of the monitor the window is on.</summary>
    public static (double X, double Y) DpiScale(Window window)
    {
        var source = PresentationSource.FromVisual(window);
        if (source?.CompositionTarget is { } target)
        {
            var m = target.TransformToDevice;
            return (m.M11, m.M22);
        }

        var dpi = VisualTreeHelper.GetDpi(window);
        return (dpi.DpiScaleX, dpi.DpiScaleY);
    }

    /// <summary>Monitor and work-area rectangles (device pixels) for the monitor nearest the window.</summary>
    public static (NativeMethods.RECT Monitor, NativeMethods.RECT Work) MonitorRects(Window window)
    {
        var hmon = NativeMethods.MonitorFromWindow(Handle(window), NativeMethods.MONITOR_DEFAULTTONEAREST);
        var info = new NativeMethods.MONITORINFO
        {
            cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MONITORINFO>(),
        };
        if (hmon != IntPtr.Zero && NativeMethods.GetMonitorInfo(hmon, ref info))
        {
            return (info.rcMonitor, info.rcWork);
        }

        var (sx, sy) = DpiScale(window);
        var work = SystemParameters.WorkArea;
        var rect = new NativeMethods.RECT
        {
            Left = (int)(work.Left * sx),
            Top = (int)(work.Top * sy),
            Right = (int)(work.Right * sx),
            Bottom = (int)(work.Bottom * sy),
        };
        return (rect, rect);
    }

    /// <summary>Moves the window without activating it, in device pixels.</summary>
    public static void MoveTo(Window window, int x, int y, int width, int height)
    {
        NativeMethods.SetWindowPos(Handle(window), IntPtr.Zero, x, y, width, height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOZORDER);
    }
}
