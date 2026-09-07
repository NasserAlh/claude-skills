using System.Windows;
using Microsoft.Win32;
using OpenOsk.Core.Settings;

namespace OpenOsk.Services;

/// <summary>Swaps the colour dictionary at runtime and follows the Windows light/dark setting.</summary>
internal static class ThemeManager
{
    private static ResourceDictionary? _current;

    public static OskTheme Requested { get; private set; } = OskTheme.System;

    public static OskTheme Effective { get; private set; } = OskTheme.Light;

    public static void Apply(OskTheme theme)
    {
        Requested = theme;
        var effective = theme == OskTheme.System ? DetectSystemTheme() : theme;
        var uri = new Uri($"Themes/{effective}.xaml", UriKind.Relative);
        var dictionary = (ResourceDictionary)Application.LoadComponent(uri);
        var merged = Application.Current.Resources.MergedDictionaries;
        if (_current is not null)
        {
            merged.Remove(_current);
        }
        else
        {
            // First call: drop the design-time theme that App.xaml merges so only one palette is active.
            for (var i = merged.Count - 1; i >= 0; i--)
            {
                var source = merged[i].Source?.OriginalString ?? string.Empty;
                if (source.StartsWith("Themes/", StringComparison.OrdinalIgnoreCase) && !source.EndsWith("Styles.xaml", StringComparison.OrdinalIgnoreCase))
                {
                    merged.RemoveAt(i);
                }
            }
        }

        merged.Insert(0, dictionary);
        _current = dictionary;
        Effective = effective;
    }

    /// <summary>Re-applies the theme if it follows the system and the system changed.</summary>
    public static void Refresh()
    {
        if (Requested == OskTheme.System && DetectSystemTheme() != Effective)
        {
            Apply(OskTheme.System);
        }
    }

    public static OskTheme DetectSystemTheme()
    {
        if (SystemParameters.HighContrast)
        {
            return OskTheme.HighContrast;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int light && light == 0 ? OskTheme.Dark : OskTheme.Light;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or IOException or UnauthorizedAccessException)
        {
            return OskTheme.Light;
        }
    }
}
