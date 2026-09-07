using System.Windows.Threading;
using OpenOsk.Core.Keys;
using OpenOsk.Native;

namespace OpenOsk.Input;

/// <summary>
/// Mirrors the real keyboard state (lock lights, physically held modifiers, active input language)
/// by polling a few cheap user32 calls on a timer. Polling instead of a low-level keyboard hook is a
/// deliberate design choice: hooks run inside every keystroke on the machine and are the classic
/// cause of the "keyboard stops responding" freezes.
/// </summary>
internal sealed class KeyboardStateMonitor : IDisposable
{
    private readonly DispatcherTimer _timer;

    public KeyboardStateMonitor(Dispatcher dispatcher)
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(100),
        };
        _timer.Tick += (_, _) => Refresh();
        Refresh();
    }

    public event EventHandler? Changed;

    public bool CapsLock { get; private set; }

    public bool NumLock { get; private set; }

    public bool ScrollLock { get; private set; }

    public bool ShiftHeld { get; private set; }

    public bool ControlHeld { get; private set; }

    public bool AltHeld { get; private set; }

    public bool WinHeld { get; private set; }

    /// <summary>Keyboard layout handle (HKL) of the foreground application's thread.</summary>
    public IntPtr InputLayout { get; private set; }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public void Dispose() => _timer.Stop();

    /// <summary>Reads everything immediately. Safe to call from the UI thread at any time.</summary>
    public void Refresh()
    {
        var changed = false;
        CapsLock = Update(CapsLock, IsToggled(VirtualKey.Capital), ref changed);
        NumLock = Update(NumLock, IsToggled(VirtualKey.NumLock), ref changed);
        ScrollLock = Update(ScrollLock, IsToggled(VirtualKey.Scroll), ref changed);
        ShiftHeld = Update(ShiftHeld, IsHeld(VirtualKey.Shift), ref changed);
        ControlHeld = Update(ControlHeld, IsHeld(VirtualKey.Control), ref changed);
        AltHeld = Update(AltHeld, IsHeld(VirtualKey.Menu), ref changed);
        WinHeld = Update(WinHeld, IsHeld(VirtualKey.LWin) || IsHeld(VirtualKey.RWin), ref changed);

        var layout = CurrentForegroundLayout();
        if (layout != InputLayout)
        {
            InputLayout = layout;
            changed = true;
        }

        if (changed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public static IntPtr CurrentForegroundLayout()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        var thread = foreground == IntPtr.Zero ? 0u : NativeMethods.GetWindowThreadProcessId(foreground, out _);
        return NativeMethods.GetKeyboardLayout(thread);
    }

    private static bool Update(bool current, bool value, ref bool changed)
    {
        if (current != value)
        {
            changed = true;
        }

        return value;
    }

    private static bool IsToggled(VirtualKey key) => (NativeMethods.GetKeyState((int)key) & 0x0001) != 0;

    private static bool IsHeld(VirtualKey key) => (NativeMethods.GetAsyncKeyState((int)key) & 0x8000) != 0;
}
