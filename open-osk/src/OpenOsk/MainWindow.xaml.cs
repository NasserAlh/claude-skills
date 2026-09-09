using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using OpenOsk.Controls;
using OpenOsk.Core.Input;
using OpenOsk.Core.Keys;
using OpenOsk.Core.Layout;
using OpenOsk.Core.Modifiers;
using OpenOsk.Core.Prediction;
using OpenOsk.Core.Settings;
using OpenOsk.Core.Typing;
using OpenOsk.Input;
using OpenOsk.Layout;
using OpenOsk.Native;
using OpenOsk.Services;

namespace OpenOsk;

/// <summary>
/// The keyboard window. It never takes focus, so every key it sends lands in whatever application
/// the user was working in.
/// </summary>
public partial class MainWindow : Window
{
    private readonly SettingsStore _settingsStore;
    private readonly ModifierController _modifiers = new();
    private readonly KeyLabelProvider _labels = new();
    private readonly WordPredictor _predictor = new();
    private readonly TypedWordTracker _typed = new();
    private readonly LearnedWordsStore _learnedStore = new(AppPaths.LearnedWordsFile);
    private readonly DwellTracker _dwell;
    private readonly ScanController _scan = new();
    private readonly KeyRepeater _repeater = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(33));
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _dwellTimer;
    private readonly DispatcherTimer _scanTimer;
    private readonly DispatcherTimer _repeatTimer;
    private readonly DispatcherTimer _saveTimer;
    private readonly Dictionary<string, KeyButton> _buttons = new(StringComparer.Ordinal);
    private readonly HashSet<string> _highlighted = new(StringComparer.Ordinal);
    private readonly ClickSound _click = new();
    private readonly SendInputInjector _injector;
    private readonly bool _startDocked;

    private OskSettings _settings;
    private KeyboardLayout _layout;
    private bool _navigation;
    private KeyboardStateMonitor? _monitor;
    private GlobalHotKey? _hotKey;
    private AppBar? _appBar;
    private KeyButton? _pressedButton;
    private KeyButton? _dwellSuppressed;
    private KeyStrokePlan? _pressedPlan;
    private bool _faded;
    private bool _pointerOver;
    private bool _docked;
    private WindowPlacement? _undockedPlacement;
    private bool _learnedDirty;
    private bool _closing;

    public MainWindow(OskSettings settings, SettingsStore settingsStore, bool startNavigation, bool startDocked)
    {
        _settings = settings;
        _settingsStore = settingsStore;
        _startDocked = startDocked;
        _navigation = startNavigation;
        _layout = _navigation ? BuiltInLayouts.Navigation : LoadLayout(settings.Layout);
        _injector = new SendInputInjector(() => _monitor?.InputLayout ?? KeyboardStateMonitor.CurrentForegroundLayout());
        _dwell = new DwellTracker(TimeSpan.FromSeconds(settings.HoverSeconds));

        InitializeComponent();

        _dwellTimer = new DispatcherTimer(DispatcherPriority.Render, Dispatcher) { Interval = TimeSpan.FromMilliseconds(33) };
        _dwellTimer.Tick += OnDwellTick;
        _scanTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher) { Interval = TimeSpan.FromSeconds(settings.ScanSeconds) };
        _scanTimer.Tick += (_, _) => _scan.Tick();
        // Ticks faster than any repeat rate; the KeyRepeater decides how many repeats each tick owes.
        _repeatTimer = new DispatcherTimer(DispatcherPriority.Input, Dispatcher) { Interval = TimeSpan.FromMilliseconds(16) };
        _repeatTimer.Tick += OnRepeatTick;
        _saveTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = TimeSpan.FromSeconds(2) };
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); SavePlacement(); SaveSettings(); SaveLearnedWords(); };

        _modifiers.AllowLock = settings.AllowModifierLock;
        _modifiers.Changed += (_, _) => { RefreshLabels(); RefreshKeyVisuals(); };
        _scan.HighlightChanged += (_, _) => RefreshScanHighlight();
        _typed.WordCompleted += OnWordCompleted;

        LoadPrediction();
        RestorePlacement();
        BuildKeyboard();

        MouseEnter += (_, _) => { _pointerOver = true; ApplyOpacity(); };
        MouseLeave += (_, _) => { _pointerOver = false; ApplyOpacity(); };
        PreviewMouseLeftButtonDown += OnWindowPreviewMouseDown;
        LocationChanged += (_, _) => ScheduleSave();
        SizeChanged += (_, _) => ScheduleSave();
        Closing += OnClosing;
    }

    /// <summary>Another OpenOSK process was started; bring this one back instead.</summary>
    public void ShowFromOtherInstance()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Show();
        Topmost = true;
    }

    // ---------------------------------------------------------------------------------------------
    // Window lifecycle
    // ---------------------------------------------------------------------------------------------

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowHelper.MakeNonActivating(this);
        var source = (HwndSource)PresentationSource.FromVisual(this)!;
        source.AddHook(WndProc);

        _monitor = new KeyboardStateMonitor(Dispatcher);
        _monitor.Changed += (_, _) => { RefreshLabels(); RefreshKeyVisuals(); };
        _monitor.Start();

        _hotKey = new GlobalHotKey(source);
        _hotKey.Pressed += (_, _) => ScanSelect();

        _appBar = new AppBar(this);

        ApplySettings(save: false);
        if (_startDocked)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => SetDocked(true));
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_MOUSEACTIVATE)
        {
            handled = true;
            return new IntPtr(NativeMethods.MA_NOACTIVATE);
        }

        return IntPtr.Zero;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _closing = true;
        ReleaseKey();
        _saveTimer.Stop();
        _dwellTimer.Stop();
        _scanTimer.Stop();
        _repeatTimer.Stop();
        SavePlacement();
        SaveSettings();
        SaveLearnedWords();
        _hotKey?.Dispose();
        _appBar?.Dispose();
        _monitor?.Dispose();
        _click.Dispose();
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnOptionsClick(object sender, RoutedEventArgs e) => ShowOptions();

    // ---------------------------------------------------------------------------------------------
    // Building the keyboard
    // ---------------------------------------------------------------------------------------------

    private static KeyboardLayout LoadLayout(string id)
    {
        try
        {
            var custom = Path.Combine(AppPaths.LayoutsDirectory, id + ".json");
            if (File.Exists(custom))
            {
                return LayoutParser.Parse(File.ReadAllText(custom));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or LayoutFormatException)
        {
            // Fall through to the built-in layout; a broken custom file must not prevent typing.
        }

        return BuiltInLayouts.Get(id);
    }

    private void BuildKeyboard()
    {
        ReleaseKey();
        _buttons.Clear();
        _highlighted.Clear();
        BuildBlock(MainBlock, _layout.Rows);
        BuildBlock(NumPadBlock, _layout.NumPadRows);
        BuildBlock(CommandBlock, _layout.CommandRows);
        UpdateBlockVisibility();
        RefreshLabels();
        RefreshKeyVisuals();
        UpdateFontSize();
        ModeLabel.Text = _navigation ? "Navigation" : string.Empty;
        if (_scan.IsRunning)
        {
            _scan.Start(_layout.ScanRows(_settings.ShowNumPad && _layout.NumPadRows.Count > 0));
        }
    }

    private void BuildBlock(Grid host, IReadOnlyList<IReadOnlyList<KeyDefinition>> rows)
    {
        host.Children.Clear();
        host.RowDefinitions.Clear();
        for (var r = 0; r < rows.Count; r++)
        {
            host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var rowGrid = new Grid();
            for (var c = 0; c < rows[r].Count; c++)
            {
                var key = rows[r][c];
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(key.Width, GridUnitType.Star) });
                if (key.IsGap)
                {
                    continue;
                }

                var button = new KeyButton(key);
                Grid.SetColumn(button, c);
                button.PreviewMouseLeftButtonDown += OnKeyMouseDown;
                button.PreviewMouseLeftButtonUp += OnKeyMouseUp;
                button.LostMouseCapture += OnKeyLostCapture;
                button.MouseEnter += OnKeyMouseEnter;
                button.MouseLeave += OnKeyMouseLeave;
                rowGrid.Children.Add(button);
                _buttons[key.Id] = button;
            }

            Grid.SetRow(rowGrid, r);
            host.Children.Add(rowGrid);
        }
    }

    private void UpdateBlockVisibility()
    {
        var showNumPad = _settings.ShowNumPad && _layout.NumPadRows.Count > 0;
        var showCommands = _settings.ShowCommandKeys && _layout.CommandRows.Count > 0;
        MainColumn.Width = new GridLength(_layout.MainWidthUnits, GridUnitType.Star);
        NumPadColumn.Width = showNumPad ? new GridLength(_layout.NumPadWidthUnits, GridUnitType.Star) : new GridLength(0);
        CommandColumn.Width = showCommands ? new GridLength(_layout.CommandWidthUnits, GridUnitType.Star) : new GridLength(0);
        NumPadBlock.Visibility = showNumPad ? Visibility.Visible : Visibility.Collapsed;
        CommandBlock.Visibility = showCommands ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnKeysHostSizeChanged(object sender, SizeChangedEventArgs e) => UpdateFontSize();

    private void UpdateFontSize()
    {
        if (_layout.Rows.Count == 0 || KeysHost.ActualHeight <= 0)
        {
            return;
        }

        var rowHeight = KeysHost.ActualHeight / _layout.Rows.Count;
        var unitWidth = MainBlock.ActualWidth > 0 ? MainBlock.ActualWidth / _layout.MainWidthUnits : rowHeight;
        var size = Math.Clamp(Math.Min(rowHeight * 0.36, unitWidth * 0.42), 9, 40);
        TextElement.SetFontSize(KeysHost, size);
        TextElement.SetFontSize(PredictionBar, Math.Clamp(size * 0.7, 11, 18));
    }

    // ---------------------------------------------------------------------------------------------
    // Pointer input
    // ---------------------------------------------------------------------------------------------

    private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_settings.TypingMode != TypingMode.Scan || !_scan.IsRunning)
        {
            return;
        }

        if ((_settings.ScanSelect is ScanSelectSource.MouseClick or ScanSelectSource.Both) && KeysHost.IsMouseOver)
        {
            ScanSelect();
            e.Handled = true;
        }
    }

    private void OnKeyMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not KeyButton button || _settings.TypingMode == TypingMode.Scan)
        {
            return;
        }

        e.Handled = true;
        if (_pressedButton is not null)
        {
            ReleaseKey();
        }

        button.CaptureMouse();
        PressKey(button);
    }

    private void OnKeyMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not KeyButton button)
        {
            return;
        }

        e.Handled = true;
        if (button.IsMouseCaptured)
        {
            button.ReleaseMouseCapture();
        }

        ReleaseKey();
    }

    private void OnKeyLostCapture(object sender, MouseEventArgs e)
    {
        if (ReferenceEquals(sender, _pressedButton))
        {
            ReleaseKey();
        }
    }

    private void OnKeyMouseEnter(object sender, MouseEventArgs e)
    {
        if (_settings.TypingMode == TypingMode.Hover && sender is KeyButton button && !ReferenceEquals(button, _dwellSuppressed))
        {
            _dwell.Enter(button.Key.Id, _clock.Elapsed);
        }
    }

    private void OnKeyMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is KeyButton button)
        {
            if (_dwell.Target == button.Key.Id)
            {
                _dwell.Leave();
            }

            if (ReferenceEquals(button, _dwellSuppressed))
            {
                _dwellSuppressed = null;
            }

            button.DwellProgress = 0;
        }
    }

    /// <summary>
    /// When a dialog closes, WPF raises MouseEnter for whatever key the pointer happens to rest on,
    /// which in hover mode would dwell-type that key (or reopen Options) a second later. Ignore that
    /// key until the pointer leaves it.
    /// </summary>
    private void SuppressDwellUnderPointer()
    {
        _dwell.Leave();
        _dwellSuppressed = null;

        // Mouse.GetPosition is stale here (the dialog owned the pointer), so ask Win32 instead.
        if (!NativeMethods.GetCursorPos(out var cursor))
        {
            return;
        }

        var hit = InputHitTest(PointFromScreen(new Point(cursor.X, cursor.Y))) as DependencyObject;
        while (hit is not null and not KeyButton)
        {
            hit = VisualTreeHelper.GetParent(hit);
        }

        _dwellSuppressed = hit as KeyButton;
    }

    private void OnDwellTick(object? sender, EventArgs e)
    {
        var target = _dwell.Target;
        if (target is null || !_buttons.TryGetValue(target, out var button))
        {
            return;
        }

        button.DwellProgress = _dwell.Progress(_clock.Elapsed);
        if (_dwell.Tick(_clock.Elapsed) is { } fired && _buttons.TryGetValue(fired, out var firedButton))
        {
            TapKey(firedButton);
            firedButton.DwellProgress = 0;
        }
    }

    private void ScanSelect()
    {
        if (!_scan.IsRunning)
        {
            return;
        }

        if (_scan.Select() is { } id && _buttons.TryGetValue(id, out var button))
        {
            TapKey(button);
        }

        // Restart the interval so the user gets a full period to react after each press.
        _scanTimer.Stop();
        _scanTimer.Start();
    }

    // ---------------------------------------------------------------------------------------------
    // Sending keys
    // ---------------------------------------------------------------------------------------------

    private void TapKey(KeyButton button)
    {
        PressKey(button);
        ReleaseKey();
    }

    private void PressKey(KeyButton button)
    {
        var key = button.Key;
        _click.Play();

        switch (key.Kind)
        {
            case KeyKind.Modifier when key.Modifier is { } modifier:
                _modifiers.Tap(modifier);
                return;
            case KeyKind.Gap:
                return;
            case KeyKind.Command:
                // Commands run on release, once the pointer capture is gone (Options opens a dialog).
                _pressedButton = button;
                _pressedPlan = null;
                button.IsPressed = true;
                return;
            default:
                break;
        }

        var plan = KeyStrokePlanner.Plan(key, _modifiers, numLock: _monitor?.NumLock ?? true);
        if (plan.Press.Count == 0)
        {
            return;
        }

        TrackTyping(key);
        _injector.Send(plan.Press);
        _pressedButton = button;
        _pressedPlan = plan;
        button.IsPressed = true;

        if (_settings.KeyRepeat && plan.Repeat.Count > 0)
        {
            _repeater.Delay = RepeatDelay();
            _repeater.Interval = RepeatInterval();
            _repeater.Start(_clock.Elapsed);
            _repeatTimer.Start();
        }
    }

    private void ReleaseKey()
    {
        _repeatTimer.Stop();
        _repeater.Stop();
        if (_pressedButton is null)
        {
            return;
        }

        var button = _pressedButton;
        var plan = _pressedPlan;
        _pressedButton = null;
        _pressedPlan = null;
        button.IsPressed = false;

        if (plan is null)
        {
            if (button.Key.Kind == KeyKind.Command && !_closing)
            {
                var command = button.Key.Command;
                Dispatcher.BeginInvoke(DispatcherPriority.Input, () => ExecuteCommand(command));
            }

            return;
        }

        _injector.Send(plan.Release);
        if (plan.ConsumesLatchedModifiers)
        {
            _modifiers.ConsumeLatched();
        }

        if (button.Key.Kind == KeyKind.Lock)
        {
            // The toggle state updates once the OS has processed the stroke; re-read shortly after.
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => _monitor?.Refresh());
        }

        UpdatePredictions();
    }

    private void OnRepeatTick(object? sender, EventArgs e)
    {
        if (_pressedButton is null || _pressedPlan is null)
        {
            _repeatTimer.Stop();
            _repeater.Stop();
            return;
        }

        var due = _repeater.Due(_clock.Elapsed);
        if (due == 0)
        {
            return;
        }

        // Late ticks owe more than one repeat; send them as one batch so they cannot interleave.
        var strokes = new List<KeyStroke>(due * _pressedPlan.Repeat.Count);
        for (var i = 0; i < due; i++)
        {
            TrackTyping(_pressedButton.Key);
            strokes.AddRange(_pressedPlan.Repeat);
        }

        _injector.Send(strokes);
    }

    /// <summary>Initial auto-repeat delay from the user's Windows keyboard settings (250 ms to 1 s).</summary>
    private static TimeSpan RepeatDelay() =>
        TimeSpan.FromMilliseconds(250 * (Math.Clamp(SystemParameters.KeyboardDelay, 0, 3) + 1));

    /// <summary>Auto-repeat interval from the Windows keyboard settings (about 2.5 to 30 per second).</summary>
    private static TimeSpan RepeatInterval()
    {
        var speed = Math.Clamp(SystemParameters.KeyboardSpeed, 0, 31);
        var perSecond = 2.5 + (speed * (27.5 / 31.0));
        return TimeSpan.FromMilliseconds(1000.0 / perSecond);
    }

    // ---------------------------------------------------------------------------------------------
    // Labels and visual state
    // ---------------------------------------------------------------------------------------------

    private bool ShiftActive => _modifiers.IsActive(ModifierKey.Shift) || (_monitor?.ShiftHeld ?? false);

    private bool CapsLockOn => _monitor?.CapsLock ?? false;

    private IntPtr InputLayout => _monitor?.InputLayout ?? KeyboardStateMonitor.CurrentForegroundLayout();

    private void RefreshLabels()
    {
        var shift = ShiftActive;
        var caps = CapsLockOn;
        var fn = _modifiers.IsActive(ModifierKey.Fn);
        var layout = InputLayout;

        foreach (var button in _buttons.Values)
        {
            var key = button.Key;
            if (key.Kind != KeyKind.Character)
            {
                continue;
            }

            if (fn && key.FnVirtualKey != VirtualKey.None)
            {
                button.Label = key.FnVirtualKey.ToString();
                button.SubLabel = string.Empty;
                continue;
            }

            var text = _labels.Character(key.VirtualKey, shift, caps, layout) ?? key.Label;
            button.Label = text;
            if (VirtualKeyInfo.IsLetter(key.VirtualKey))
            {
                button.SubLabel = string.Empty;
            }
            else
            {
                var alternate = _labels.Character(key.VirtualKey, !shift, caps, layout);
                button.SubLabel = alternate is not null && alternate != text ? alternate : string.Empty;
            }
        }
    }

    private void RefreshKeyVisuals()
    {
        foreach (var button in _buttons.Values)
        {
            var key = button.Key;
            switch (key.Kind)
            {
                case KeyKind.Modifier when key.Modifier is { } modifier:
                    var state = _modifiers[modifier];
                    var held = modifier switch
                    {
                        ModifierKey.Shift => _monitor?.ShiftHeld ?? false,
                        ModifierKey.Control => _monitor?.ControlHeld ?? false,
                        ModifierKey.Alt => _monitor?.AltHeld ?? false,
                        ModifierKey.Win => _monitor?.WinHeld ?? false,
                        _ => false,
                    };
                    button.IsLatched = state == LatchState.Latched || held;
                    button.IsLocked = state == LatchState.Locked;
                    break;
                case KeyKind.Lock:
                    button.IsLocked = key.VirtualKey switch
                    {
                        VirtualKey.Capital => CapsLockOn,
                        VirtualKey.NumLock => _monitor?.NumLock ?? false,
                        VirtualKey.Scroll => _monitor?.ScrollLock ?? false,
                        _ => false,
                    };
                    break;
                case KeyKind.Command:
                    button.IsLatched = key.Command switch
                    {
                        OskCommand.Fade => _faded,
                        OskCommand.Dock => _docked,
                        OskCommand.NumPad => _settings.ShowNumPad,
                        _ => false,
                    };
                    break;
                default:
                    break;
            }
        }
    }

    private void RefreshScanHighlight()
    {
        foreach (var id in _highlighted)
        {
            if (_buttons.TryGetValue(id, out var old))
            {
                old.IsHighlighted = false;
            }
        }

        _highlighted.Clear();
        foreach (var id in _scan.Highlighted)
        {
            if (_buttons.TryGetValue(id, out var button))
            {
                button.IsHighlighted = true;
                _highlighted.Add(id);
            }
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Text prediction
    // ---------------------------------------------------------------------------------------------

    private void LoadPrediction()
    {
        var lexicon = BuiltInLexicons.Read(System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
                      ?? BuiltInLexicons.Read("en");
        if (lexicon is not null)
        {
            _predictor.LoadLexicon(lexicon);
        }

        _predictor.LoadLearned(_learnedStore.Load());
    }

    private void TrackTyping(KeyDefinition key)
    {
        if (_modifiers.IsActive(ModifierKey.Control) || _modifiers.IsActive(ModifierKey.Alt) || _modifiers.IsActive(ModifierKey.Win)
            || (_monitor?.ControlHeld ?? false) || (_monitor?.AltHeld ?? false))
        {
            _typed.Reset();
            return;
        }

        switch (key.Kind)
        {
            case KeyKind.Character:
                if (_modifiers.IsActive(ModifierKey.Fn) && key.FnVirtualKey != VirtualKey.None)
                {
                    _typed.Reset();
                    return;
                }

                var text = _labels.Character(key.VirtualKey, ShiftActive, CapsLockOn, InputLayout);
                if (text is { Length: 1 })
                {
                    _typed.OnCharacter(text[0]);
                }
                else
                {
                    _typed.Break();
                }

                break;
            case KeyKind.Action:
                switch (key.VirtualKey)
                {
                    case VirtualKey.Back:
                        _typed.OnBackspace();
                        break;
                    case VirtualKey.Space:
                    case VirtualKey.Return:
                    case VirtualKey.Tab:
                        _typed.Break();
                        break;
                    case VirtualKey.Escape:
                    case VirtualKey.Left:
                    case VirtualKey.Right:
                    case VirtualKey.Up:
                    case VirtualKey.Down:
                    case VirtualKey.Home:
                    case VirtualKey.End:
                    case VirtualKey.Prior:
                    case VirtualKey.Next:
                    case VirtualKey.Delete:
                        _typed.Reset();
                        break;
                    default:
                        if (VirtualKeyInfo.IsNumPad(key.VirtualKey))
                        {
                            _typed.Break();
                        }

                        break;
                }

                break;
            default:
                break;
        }
    }

    private void OnWordCompleted(object? sender, string word)
    {
        if (_settings.LearnWords)
        {
            _predictor.Learn(word);
            _learnedDirty = true;
            ScheduleSave();
        }
    }

    private void UpdatePredictions()
    {
        if (!_settings.ShowPredictions)
        {
            PredictionBar.Visibility = Visibility.Collapsed;
            return;
        }

        PredictionBar.Visibility = Visibility.Visible;
        var prefix = _typed.CurrentPrefix;
        var words = prefix.Length == 0 ? [] : _predictor.Predict(prefix, _settings.PredictionCount);
        var count = _settings.PredictionCount;
        while (PredictionPanel.Children.Count < count)
        {
            var chip = new Button { Style = (Style)FindResource("PredictionButton") };
            chip.Click += OnPredictionClick;
            PredictionPanel.Children.Add(chip);
        }

        while (PredictionPanel.Children.Count > count)
        {
            PredictionPanel.Children.RemoveAt(PredictionPanel.Children.Count - 1);
        }

        PredictionPanel.Columns = count;
        for (var i = 0; i < count; i++)
        {
            var chip = (Button)PredictionPanel.Children[i];
            if (i < words.Count)
            {
                chip.Content = words[i];
                chip.Visibility = Visibility.Visible;
            }
            else
            {
                chip.Content = string.Empty;
                chip.Visibility = Visibility.Hidden;
            }
        }
    }

    private void OnPredictionClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Content: string word } || word.Length == 0)
        {
            return;
        }

        _click.Play();
        var suffix = WordPredictor.Completion(_typed.CurrentPrefix, word);
        var text = _settings.InsertSpaceAfterPrediction ? suffix + " " : suffix;
        _injector.SendText(text);
        _typed.OnText(text);
        _modifiers.ConsumeLatched();
        UpdatePredictions();
    }

    private void SaveLearnedWords()
    {
        if (!_learnedDirty)
        {
            return;
        }

        try
        {
            _learnedStore.Save(_predictor.LearnedWords);
            _learnedDirty = false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Learned words are a convenience; losing them is not worth interrupting the user.
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Commands (Nav, Mv Up, Mv Dn, Dock, Fade, Options, Help)
    // ---------------------------------------------------------------------------------------------

    private void ExecuteCommand(OskCommand command)
    {
        if (_closing)
        {
            return;
        }

        switch (command)
        {
            case OskCommand.Navigation:
                SwitchLayout(navigation: true);
                break;
            case OskCommand.General:
                SwitchLayout(navigation: false);
                break;
            case OskCommand.MoveUp:
                MoveToEdge(top: true);
                break;
            case OskCommand.MoveDown:
                MoveToEdge(top: false);
                break;
            case OskCommand.Dock:
                SetDocked(!_docked);
                break;
            case OskCommand.Fade:
                _faded = !_faded;
                ApplyOpacity();
                RefreshKeyVisuals();
                break;
            case OskCommand.Options:
                ShowOptions();
                break;
            case OskCommand.Help:
                new HelpWindow { Owner = this }.Show();
                break;
            case OskCommand.NumPad:
                _settings.ShowNumPad = !_settings.ShowNumPad;
                ApplySettings(save: true);
                break;
            default:
                break;
        }
    }

    private void SwitchLayout(bool navigation)
    {
        if (_navigation == navigation)
        {
            return;
        }

        SavePlacement();
        _navigation = navigation;
        _layout = navigation ? BuiltInLayouts.Navigation : LoadLayout(_settings.Layout);
        _modifiers.ReleaseAll();
        _typed.Reset();
        BuildKeyboard();
        UpdatePredictions();

        if (!_docked)
        {
            var placement = navigation ? _settings.NavigationWindow : _settings.Window;
            if (placement is not null && IsOnScreen(placement))
            {
                Apply(placement);
            }
            else if (navigation)
            {
                // First time in navigation mode: keep the position, use a compact size.
                Width = Math.Max(MinWidth, Width * 0.55);
                Height = Math.Max(MinHeight, Height * 0.9);
            }
        }
        else
        {
            SetDocked(true);
        }
    }

    private void MoveToEdge(bool top)
    {
        if (_docked)
        {
            SetDocked(false);
        }

        var (_, work) = WindowHelper.MonitorRects(this);
        var (sx, sy) = WindowHelper.DpiScale(this);
        var width = (int)Math.Round(ActualWidth * sx);
        var height = (int)Math.Round(ActualHeight * sy);
        var x = (int)Math.Clamp(Math.Round(Left * sx), work.Left, Math.Max(work.Left, work.Right - width));
        var y = top ? work.Top : work.Bottom - height;
        WindowHelper.MoveTo(this, x, y, width, height);
    }

    private void SetDocked(bool docked)
    {
        if (_appBar is null)
        {
            return;
        }

        if (docked)
        {
            if (!_docked)
            {
                _undockedPlacement = CurrentPlacement();
            }

            var (_, sy) = WindowHelper.DpiScale(this);
            var height = (int)Math.Round(ActualHeight * sy);
            _docked = true;
            ResizeMode = ResizeMode.NoResize;
            Chrome.CaptionHeight = 0;
            _appBar.Dock(height);
        }
        else
        {
            _appBar.Undock();
            _docked = false;
            ResizeMode = ResizeMode.CanResize;
            Chrome.CaptionHeight = 30;
            if (_undockedPlacement is { } placement && IsOnScreen(placement))
            {
                Apply(placement);
            }
        }

        RefreshKeyVisuals();
    }

    private void ApplyOpacity()
    {
        // WPF strips WS_EX_LAYERED from any window that does not use per-pixel opacity, so
        // SetLayeredWindowAttributes cannot fade this window; AllowsTransparency plus Opacity can.
        Opacity = _faded && !_pointerOver ? _settings.FadeOpacity : 1.0;
    }

    private void ShowOptions()
    {
        ReleaseKey();
        var dialog = new OptionsWindow(_settings, _layout.Id) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            var layoutChanged = !_navigation && dialog.Result.Layout != _settings.Layout;
            _settings = dialog.Result;
            if (dialog.ForgetLearnedWords)
            {
                _predictor.ForgetAll();
                _learnedDirty = false;
            }

            if (layoutChanged)
            {
                _layout = LoadLayout(_settings.Layout);
                BuildKeyboard();
            }

            ApplySettings(save: true);
        }

        SuppressDwellUnderPointer();
    }

    // ---------------------------------------------------------------------------------------------
    // Settings
    // ---------------------------------------------------------------------------------------------

    private void ApplySettings(bool save)
    {
        _click.Enabled = _settings.ClickSound;
        _dwell.DwellTime = TimeSpan.FromSeconds(_settings.HoverSeconds);
        _scanTimer.Interval = TimeSpan.FromSeconds(_settings.ScanSeconds);
        _modifiers.AllowLock = _settings.AllowModifierLock;
        ThemeManager.Apply(_settings.Theme);
        UpdateBlockVisibility();
        UpdateFontSize();
        ApplyOpacity();
        RefreshKeyVisuals();
        UpdatePredictions();

        // Typing mode
        _dwell.Leave();
        _dwellTimer.Stop();
        _scanTimer.Stop();
        _scan.Stop();
        _hotKey?.Unregister();
        foreach (var button in _buttons.Values)
        {
            button.DwellProgress = 0;
        }

        switch (_settings.TypingMode)
        {
            case TypingMode.Hover:
                _dwellTimer.Start();
                ModeLabel.Text = _navigation ? "Navigation · Hover" : "Hover";
                break;
            case TypingMode.Scan:
                _scan.Start(_layout.ScanRows(_settings.ShowNumPad && _layout.NumPadRows.Count > 0));
                _scanTimer.Start();
                if (_settings.ScanSelect is ScanSelectSource.KeyboardKey or ScanSelectSource.Both)
                {
                    _hotKey?.Register(_settings.ScanSelectKey);
                }

                ModeLabel.Text = _navigation ? "Navigation · Scan" : "Scan";
                break;
            default:
                ModeLabel.Text = _navigation ? "Navigation" : string.Empty;
                break;
        }

        if (save)
        {
            SaveSettings();
        }
    }

    private void ScheduleSave()
    {
        if (_closing || !IsLoaded)
        {
            return;
        }

        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveSettings()
    {
        try
        {
            _settingsStore.Save(_settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Settings will simply be defaults next time; keep the keyboard usable now.
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Window placement
    // ---------------------------------------------------------------------------------------------

    private WindowPlacement CurrentPlacement() => new(Left, Top, Width, Height);

    private void Apply(WindowPlacement placement)
    {
        Left = placement.Left;
        Top = placement.Top;
        Width = placement.Width;
        Height = placement.Height;
    }

    private static bool IsOnScreen(WindowPlacement p)
    {
        var screen = new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        var rect = new Rect(p.Left, p.Top, Math.Max(p.Width, 1), Math.Max(p.Height, 1));
        rect.Intersect(screen);
        return !rect.IsEmpty && rect.Width >= 100 && rect.Height >= 60;
    }

    private void RestorePlacement()
    {
        var saved = _navigation ? _settings.NavigationWindow : _settings.Window;
        if (saved is not null && IsOnScreen(saved))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Apply(saved);
            return;
        }

        var work = SystemParameters.WorkArea;
        var width = Math.Clamp(work.Width * (_navigation ? 0.4 : 0.62), MinWidth, 1400);
        var height = Math.Clamp(width * (_navigation ? 0.45 : 0.34), MinHeight, work.Height * 0.5);
        WindowStartupLocation = WindowStartupLocation.Manual;
        Width = width;
        Height = height;
        Left = work.Left + ((work.Width - width) / 2);
        Top = work.Bottom - height - 8;
    }

    private void SavePlacement()
    {
        if (_docked || WindowState != WindowState.Normal)
        {
            return;
        }

        if (_navigation)
        {
            _settings.NavigationWindow = CurrentPlacement();
        }
        else
        {
            _settings.Window = CurrentPlacement();
        }
    }
}
