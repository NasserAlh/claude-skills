namespace OpenOsk.Services;

/// <summary>Where OpenOSK keeps its (small, local, plain-text) state.</summary>
internal static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenOSK");

    public static string SettingsFile => Path.Combine(DataDirectory, "settings.json");

    public static string LearnedWordsFile => Path.Combine(DataDirectory, "learned-words.txt");

    public static string ErrorLogFile => Path.Combine(DataDirectory, "error.log");

    /// <summary>User-supplied layouts: any <c>*.json</c> in this folder is offered in Options.</summary>
    public static string LayoutsDirectory => Path.Combine(DataDirectory, "layouts");
}
