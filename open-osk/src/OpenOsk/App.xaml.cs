using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using OpenOsk.Core.Settings;
using OpenOsk.Services;

namespace OpenOsk;

public partial class App : Application
{
    private SingleInstance? _instance;
    private MainWindow? _main;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => LogError(args.ExceptionObject as Exception);

        _instance = new SingleInstance();
        if (!_instance.IsFirst)
        {
            _instance.RequestShow();
            Shutdown();
            return;
        }

        var startNavigation = false;
        var startDocked = false;
        foreach (var arg in e.Args)
        {
            switch (arg.ToLowerInvariant())
            {
                case "--nav":
                case "/nav":
                    startNavigation = true;
                    break;
                case "--dock":
                case "/dock":
                    startDocked = true;
                    break;
                default:
                    break;
            }
        }

        var store = new SettingsStore(AppPaths.SettingsFile);
        var settings = store.Load();
        ThemeManager.Apply(settings.Theme);
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        _main = new MainWindow(settings, store, startNavigation || settings.StartInNavigationMode, startDocked || settings.StartDocked);
        MainWindow = _main;
        _instance.OnShowRequested(() => Dispatcher.BeginInvoke(() => _main.ShowFromOtherInstance()));
        _main.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _instance?.Dispose();
        base.OnExit(e);
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.Accessibility or UserPreferenceCategory.VisualStyle)
        {
            Dispatcher.BeginInvoke(ThemeManager.Refresh);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogError(e.Exception);
        // A keyboard that vanishes leaves the user without any way to type. Log, tell them, keep going.
        e.Handled = true;
        MessageBox.Show(
            $"OpenOSK hit an unexpected error and kept running.\n\n{e.Exception.Message}\n\nDetails were written to:\n{AppPaths.ErrorLogFile}",
            "OpenOSK",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private static void LogError(Exception? exception)
    {
        if (exception is null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);
            File.AppendAllText(AppPaths.ErrorLogFile, $"{DateTimeOffset.Now:O} {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Nowhere to log to; nothing else to do.
        }
    }
}
