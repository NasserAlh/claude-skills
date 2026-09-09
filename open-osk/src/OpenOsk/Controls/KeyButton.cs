using System.Windows;
using System.Windows.Controls;
using OpenOsk.Core.Keys;

namespace OpenOsk.Controls;

/// <summary>
/// One on-screen key. Purely visual: it exposes state as dependency properties and leaves all
/// pointer handling to the window so the three typing modes can share one control.
/// </summary>
public sealed class KeyButton : Control
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(KeyButton), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubLabelProperty =
        DependencyProperty.Register(nameof(SubLabel), typeof(string), typeof(KeyButton), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsPressedProperty =
        DependencyProperty.Register(nameof(IsPressed), typeof(bool), typeof(KeyButton), new PropertyMetadata(false));

    public static readonly DependencyProperty IsLatchedProperty =
        DependencyProperty.Register(nameof(IsLatched), typeof(bool), typeof(KeyButton), new PropertyMetadata(false));

    public static readonly DependencyProperty IsLockedProperty =
        DependencyProperty.Register(nameof(IsLocked), typeof(bool), typeof(KeyButton), new PropertyMetadata(false));

    public static readonly DependencyProperty IsHighlightedProperty =
        DependencyProperty.Register(nameof(IsHighlighted), typeof(bool), typeof(KeyButton), new PropertyMetadata(false));

    public static readonly DependencyProperty DwellProgressProperty =
        DependencyProperty.Register(nameof(DwellProgress), typeof(double), typeof(KeyButton), new PropertyMetadata(0.0));

    public static readonly DependencyProperty KeyKindProperty =
        DependencyProperty.Register(nameof(KeyKind), typeof(KeyKind), typeof(KeyButton), new PropertyMetadata(KeyKind.Action));

    public static readonly DependencyProperty SubFontSizeProperty =
        DependencyProperty.Register(nameof(SubFontSize), typeof(double), typeof(KeyButton), new PropertyMetadata(8.0));

    public static readonly DependencyProperty ShowSubLabelProperty =
        DependencyProperty.Register(nameof(ShowSubLabel), typeof(bool), typeof(KeyButton), new PropertyMetadata(true));

    /// <summary>Below this main font size the corner label would collide with the main label, so it is hidden.</summary>
    public const double MinFontSizeForSubLabel = 13.0;

    static KeyButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(KeyButton), new FrameworkPropertyMetadata(typeof(KeyButton)));
        FocusableProperty.OverrideMetadata(typeof(KeyButton), new FrameworkPropertyMetadata(false));
        FontSizeProperty.OverrideMetadata(typeof(KeyButton), new FrameworkPropertyMetadata(OnFontSizeChanged));
    }

    public KeyButton(KeyDefinition key)
    {
        Key = key;
        KeyKind = key.Kind;
        Label = key.Label;
        SubLabel = key.ShiftLabel ?? string.Empty;
    }

    public KeyDefinition Key { get; }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string SubLabel
    {
        get => (string)GetValue(SubLabelProperty);
        set => SetValue(SubLabelProperty, value);
    }

    public bool IsPressed
    {
        get => (bool)GetValue(IsPressedProperty);
        set => SetValue(IsPressedProperty, value);
    }

    public bool IsLatched
    {
        get => (bool)GetValue(IsLatchedProperty);
        set => SetValue(IsLatchedProperty, value);
    }

    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    public bool IsHighlighted
    {
        get => (bool)GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
    }

    public double DwellProgress
    {
        get => (double)GetValue(DwellProgressProperty);
        set => SetValue(DwellProgressProperty, value);
    }

    public KeyKind KeyKind
    {
        get => (KeyKind)GetValue(KeyKindProperty);
        set => SetValue(KeyKindProperty, value);
    }

    public double SubFontSize
    {
        get => (double)GetValue(SubFontSizeProperty);
        set => SetValue(SubFontSizeProperty, value);
    }

    /// <summary>Whether the corner label is drawn; false when the key is too small for two labels.</summary>
    public bool ShowSubLabel
    {
        get => (bool)GetValue(ShowSubLabelProperty);
        set => SetValue(ShowSubLabelProperty, value);
    }

    private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KeyButton button)
        {
            var size = (double)e.NewValue;
            button.SubFontSize = Math.Max(7.0, size * 0.55);
            button.ShowSubLabel = size >= MinFontSizeForSubLabel;
        }
    }
}
