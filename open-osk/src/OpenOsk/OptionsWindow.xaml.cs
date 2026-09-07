using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using OpenOsk.Core.Keys;
using OpenOsk.Core.Layout;
using OpenOsk.Core.Prediction;
using OpenOsk.Core.Settings;
using OpenOsk.Core.Typing;
using OpenOsk.Services;

namespace OpenOsk;

/// <summary>The options dialog. Mirrors the Windows OSK dialog and adds OpenOSK's own settings below it.</summary>
public partial class OptionsWindow : Window
{
    private static readonly VirtualKey[] ScanKeys =
    [
        VirtualKey.Space,
        VirtualKey.Return,
        VirtualKey.Tab,
        VirtualKey.Escape,
        VirtualKey.Pause,
        VirtualKey.Scroll,
        VirtualKey.F1,
        VirtualKey.F2,
        VirtualKey.F3,
        VirtualKey.F4,
        VirtualKey.F5,
        VirtualKey.F6,
        VirtualKey.F7,
        VirtualKey.F8,
        VirtualKey.F9,
        VirtualKey.F10,
        VirtualKey.F11,
        VirtualKey.F12,
    ];

    private readonly bool _startupWasEnabled;
    private bool _forgetLearned;

    public OptionsWindow(OskSettings current, string currentLayoutId)
    {
        InitializeComponent();
        Result = current.Normalized();

        ClickSoundBox.IsChecked = Result.ClickSound;
        CommandKeysBox.IsChecked = Result.ShowCommandKeys;
        NumPadBox.IsChecked = Result.ShowNumPad;

        ClickMode.IsChecked = Result.TypingMode == TypingMode.Click;
        HoverMode.IsChecked = Result.TypingMode == TypingMode.Hover;
        ScanMode.IsChecked = Result.TypingMode == TypingMode.Scan;
        HoverSlider.Value = Result.HoverSeconds;
        ScanSlider.Value = Result.ScanSeconds;
        HoverSlider.ValueChanged += (_, _) => HoverValue.Text = Seconds(HoverSlider.Value);
        ScanSlider.ValueChanged += (_, _) => ScanValue.Text = Seconds(ScanSlider.Value);
        HoverValue.Text = Seconds(HoverSlider.Value);
        ScanValue.Text = Seconds(ScanSlider.Value);

        ScanKeyBox.IsChecked = Result.ScanSelect is ScanSelectSource.KeyboardKey or ScanSelectSource.Both;
        ScanMouseBox.IsChecked = Result.ScanSelect is ScanSelectSource.MouseClick or ScanSelectSource.Both;
        foreach (var key in ScanKeys)
        {
            ScanKeyCombo.Items.Add(new ComboBoxItem { Content = key.ToString(), Tag = key });
        }

        ScanKeyCombo.SelectedIndex = Math.Max(0, Array.IndexOf(ScanKeys, Result.ScanSelectKey));

        PredictionsBox.IsChecked = Result.ShowPredictions;
        SpaceAfterBox.IsChecked = Result.InsertSpaceAfterPrediction;
        LearnWordsBox.IsChecked = Result.LearnWords;

        foreach (var layout in AvailableLayouts())
        {
            LayoutCombo.Items.Add(new ComboBoxItem { Content = layout.Name, Tag = layout.Id });
            if (string.Equals(layout.Id, Result.Layout, StringComparison.OrdinalIgnoreCase)
                || (LayoutCombo.SelectedIndex < 0 && string.Equals(layout.Id, currentLayoutId, StringComparison.OrdinalIgnoreCase)))
            {
                LayoutCombo.SelectedIndex = LayoutCombo.Items.Count - 1;
            }
        }

        if (LayoutCombo.SelectedIndex < 0 && LayoutCombo.Items.Count > 0)
        {
            LayoutCombo.SelectedIndex = 0;
        }

        RepeatBox.IsChecked = Result.KeyRepeat;
        ModifierLockBox.IsChecked = Result.AllowModifierLock;

        foreach (var theme in Enum.GetValues<OskTheme>())
        {
            ThemeCombo.Items.Add(new ComboBoxItem { Content = ThemeName(theme), Tag = theme });
        }

        ThemeCombo.SelectedIndex = (int)Result.Theme;
        FadeSlider.Value = Result.FadeOpacity;
        FadeSlider.ValueChanged += (_, _) => FadeValue.Text = Percent(FadeSlider.Value);
        FadeValue.Text = Percent(FadeSlider.Value);

        _startupWasEnabled = StartupRegistration.IsEnabled();
        StartupBox.IsChecked = _startupWasEnabled;
        StartDockedBox.IsChecked = Result.StartDocked;
        StartNavBox.IsChecked = Result.StartInNavigationMode;
    }

    /// <summary>The settings as edited. Valid after <see cref="Window.ShowDialog"/> returns true.</summary>
    public OskSettings Result { get; private set; }

    /// <summary>True when the user asked to clear the learned-word list.</summary>
    public bool ForgetLearnedWords => _forgetLearned;

    private static string Seconds(double value) => string.Format(CultureInfo.CurrentCulture, "{0:0.0} s", value);

    private static string Percent(double value) => string.Format(CultureInfo.CurrentCulture, "{0:0}%", value * 100);

    private static string ThemeName(OskTheme theme) => theme switch
    {
        OskTheme.System => "Follow Windows setting",
        OskTheme.Light => "Light",
        OskTheme.Dark => "Dark",
        OskTheme.HighContrast => "High contrast",
        _ => theme.ToString(),
    };

    private static IEnumerable<(string Id, string Name)> AvailableLayouts()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var layout in BuiltInLayouts.All.Values)
        {
            if (layout.Id != BuiltInLayouts.NavigationId && seen.Add(layout.Id))
            {
                yield return (layout.Id, layout.Name);
            }
        }

        if (!Directory.Exists(AppPaths.LayoutsDirectory))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(AppPaths.LayoutsDirectory, "*.json"))
        {
            KeyboardLayout? layout = null;
            try
            {
                layout = LayoutParser.Parse(File.ReadAllText(file));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or LayoutFormatException)
            {
                // Skip unreadable or malformed layouts; the keyboard must not depend on them.
            }

            var id = Path.GetFileNameWithoutExtension(file);
            if (layout is not null && seen.Add(id))
            {
                yield return (id, $"{layout.Name} (custom)");
            }
        }
    }

    private void OnForgetClick(object sender, RoutedEventArgs e)
    {
        _forgetLearned = true;
        ForgetButton.IsEnabled = false;
        ForgetButton.Content = "Learned words will be cleared when you press OK";
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        var s = Result;
        s.ClickSound = ClickSoundBox.IsChecked == true;
        s.ShowCommandKeys = CommandKeysBox.IsChecked == true;
        s.ShowNumPad = NumPadBox.IsChecked == true;
        s.TypingMode = HoverMode.IsChecked == true ? TypingMode.Hover : ScanMode.IsChecked == true ? TypingMode.Scan : TypingMode.Click;
        s.HoverSeconds = HoverSlider.Value;
        s.ScanSeconds = ScanSlider.Value;
        var key = ScanKeyBox.IsChecked == true;
        var mouse = ScanMouseBox.IsChecked == true;
        s.ScanSelect = key && mouse ? ScanSelectSource.Both : mouse ? ScanSelectSource.MouseClick : ScanSelectSource.KeyboardKey;
        s.ScanSelectKey = ScanKeyCombo.SelectedItem is ComboBoxItem { Tag: VirtualKey vk } ? vk : VirtualKey.Space;
        s.ShowPredictions = PredictionsBox.IsChecked == true;
        s.InsertSpaceAfterPrediction = SpaceAfterBox.IsChecked == true;
        s.LearnWords = LearnWordsBox.IsChecked == true;
        s.Layout = LayoutCombo.SelectedItem is ComboBoxItem { Tag: string layoutId } ? layoutId : BuiltInLayouts.StandardId;
        s.KeyRepeat = RepeatBox.IsChecked == true;
        s.AllowModifierLock = ModifierLockBox.IsChecked == true;
        s.Theme = ThemeCombo.SelectedItem is ComboBoxItem { Tag: OskTheme theme } ? theme : OskTheme.System;
        s.FadeOpacity = FadeSlider.Value;
        s.StartDocked = StartDockedBox.IsChecked == true;
        s.StartInNavigationMode = StartNavBox.IsChecked == true;

        var startup = StartupBox.IsChecked == true;
        if (startup != _startupWasEnabled && !StartupRegistration.SetEnabled(startup))
        {
            MessageBox.Show(this, "Could not update the sign-in setting. You can add or remove OpenOSK in Settings > Apps > Startup instead.", "OpenOSK", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        if (_forgetLearned)
        {
            try
            {
                new LearnedWordsStore(AppPaths.LearnedWordsFile).Save([]);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Best effort; the in-memory list is cleared by the caller.
            }
        }

        Result = s.Normalized();
        DialogResult = true;
    }
}
