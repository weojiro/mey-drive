using System.Windows;
using System.Windows.Interop;

namespace MeyDrive;

public partial class App : Application
{
    const string ShowEvent = "Local\\MeyDrive-show";

    Mutex? _single;
    EventWaitHandle _show = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        // the admin copy started by VJoySetup: no window, and before the mutex the normal copy holds
        if (e.Args.Contains(VJoySetup.ElevatedArg))
        {
            Shutdown(VJoySetup.RunElevated());
            return;
        }

        _single = new Mutex(true, "Local\\MeyDrive-single-instance", out bool first);
        if (!first)
        {
            ShowRunningCopy();
            Shutdown();
            return;
        }

        // never leave the cursor hidden or clipped, whatever happens
        AppDomain.CurrentDomain.UnhandledException += (_, _) => CursorLock.Release();
        DispatcherUnhandledException += (_, _) => CursorLock.Release();

        Native.KeepTimerResolution();
        base.OnStartup(e);
        var window = new MainWindow(startEngine: e.Args.Contains("--start"));
        // started with Windows: stay in the tray, but still get a handle so the hotkey and engine come up
        if (e.Args.Contains("--tray")) new WindowInteropHelper(window).EnsureHandle();
        else window.Show();
        ListenForSecondStart(window);
    }

    // Started again while waiting in the tray: open the copy that is already running.
    static void ShowRunningCopy()
    {
        try
        {
            Native.AllowSetForegroundWindow(Native.ASFW_ANY);
            using EventWaitHandle show = EventWaitHandle.OpenExisting(ShowEvent);
            show.Set();
        }
        catch (WaitHandleCannotBeOpenedException) { }
    }

    void ListenForSecondStart(MainWindow window)
    {
        _show = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEvent);
        var thread = new Thread(() =>
        {
            while (_show.WaitOne()) Dispatcher.BeginInvoke(window.ShowFromTray);
        }) { IsBackground = true, Name = "MeyDrive second start" };
        thread.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        CursorLock.Release();
        base.OnExit(e);
    }
}
