using System.Windows;
using System.Windows.Interop;

namespace MeyDrive;

public partial class App : Application
{
    Mutex? _single;

    protected override void OnStartup(StartupEventArgs e)
    {
        _single = new Mutex(true, "Local\\MeyDrive-single-instance", out bool first);
        if (!first) { Shutdown(); return; }

        // never leave the cursor hidden or clipped, whatever happens
        AppDomain.CurrentDomain.UnhandledException += (_, _) => CursorLock.Release();
        DispatcherUnhandledException += (_, _) => CursorLock.Release();

        Native.KeepTimerResolution();
        base.OnStartup(e);
        var window = new MainWindow(startEngine: e.Args.Contains("--start"));
        // started with Windows: stay in the tray, but still get a handle so the hotkey and engine come up
        if (e.Args.Contains("--tray")) new WindowInteropHelper(window).EnsureHandle();
        else window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        CursorLock.Release();
        base.OnExit(e);
    }
}
