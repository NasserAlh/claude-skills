using OpenOsk.Core.Keys;
using OpenOsk.Core.Typing;

namespace OpenOsk.Core.Settings;

/// <summary>User settings. Persisted as JSON in the user's local profile; never leaves the machine.</summary>
public sealed class OskSettings
{
    public const int SchemaVersion = 1;

    public int Version { get; set; } = SchemaVersion;

    // --- Typing ---------------------------------------------------------------------------------

    public TypingMode TypingMode { get; set; } = TypingMode.Click;

    /// <summary>Dwell time for hover mode, in seconds. The Windows OSK range is 0.5 to 3.</summary>
    public double HoverSeconds { get; set; } = 1.0;

    /// <summary>Scan interval in seconds. The Windows OSK range is 0.5 to 3.</summary>
    public double ScanSeconds { get; set; } = 1.0;

    public ScanSelectSource ScanSelect { get; set; } = ScanSelectSource.Both;

    public VirtualKey ScanSelectKey { get; set; } = VirtualKey.Space;

    public bool KeyRepeat { get; set; } = true;

    public bool AllowModifierLock { get; set; } = true;

    // --- Sound and look ---------------------------------------------------------------------------

    public bool ClickSound { get; set; } = true;

    public bool ShowNumPad { get; set; }

    /// <summary>Show the Nav / Mv Up / Mv Dn / Dock / Fade column ("keys to move around the screen").</summary>
    public bool ShowCommandKeys { get; set; } = true;

    public OskTheme Theme { get; set; } = OskTheme.System;

    /// <summary>Window opacity while faded (0.1 to 1.0).</summary>
    public double FadeOpacity { get; set; } = 0.35;

    // --- Prediction ---------------------------------------------------------------------------------

    public bool ShowPredictions { get; set; } = true;

    public bool InsertSpaceAfterPrediction { get; set; } = true;

    public bool LearnWords { get; set; } = true;

    public int PredictionCount { get; set; } = 6;

    // --- Window ---------------------------------------------------------------------------------------

    public string Layout { get; set; } = "standard";

    public bool StartDocked { get; set; }

    public bool StartInNavigationMode { get; set; }

    public WindowPlacement? Window { get; set; }

    public WindowPlacement? NavigationWindow { get; set; }

    /// <summary>Returns a copy with every value clamped to its valid range.</summary>
    public OskSettings Normalized()
    {
        var copy = (OskSettings)MemberwiseClone();
        copy.Version = SchemaVersion;
        copy.HoverSeconds = Math.Clamp(copy.HoverSeconds, 0.5, 3.0);
        copy.ScanSeconds = Math.Clamp(copy.ScanSeconds, 0.5, 3.0);
        copy.FadeOpacity = Math.Clamp(copy.FadeOpacity, 0.1, 1.0);
        copy.PredictionCount = Math.Clamp(copy.PredictionCount, 1, 10);
        if (!Enum.IsDefined(copy.TypingMode))
        {
            copy.TypingMode = TypingMode.Click;
        }

        if (!Enum.IsDefined(copy.ScanSelect))
        {
            copy.ScanSelect = ScanSelectSource.Both;
        }

        if (!Enum.IsDefined(copy.Theme))
        {
            copy.Theme = OskTheme.System;
        }

        if (!Enum.IsDefined(copy.ScanSelectKey) || copy.ScanSelectKey == VirtualKey.None)
        {
            copy.ScanSelectKey = VirtualKey.Space;
        }

        if (string.IsNullOrWhiteSpace(copy.Layout))
        {
            copy.Layout = "standard";
        }

        return copy;
    }
}

public enum OskTheme
{
    System,
    Light,
    Dark,
    HighContrast,
}

/// <summary>Saved window rectangle in device-independent pixels.</summary>
public sealed record WindowPlacement(double Left, double Top, double Width, double Height);
