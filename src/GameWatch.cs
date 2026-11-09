using System.Diagnostics;
using System.Windows.Threading;

namespace MeyDrive;

// Polls every 2 s for Content Manager or Assetto Corsa and reports when one opens or the last one closes.
sealed class GameWatch
{
    // acs.exe is the game, AssettoCorsa.exe the Steam launcher; CM is often renamed, so match on its usual names
    static readonly string[] Names = { "acs", "AssettoCorsa", "Content Manager", "ContentManager" };

    readonly DispatcherTimer _timer;
    bool _open;

    public event Action<bool>? Changed;   // true: CM or the game opened, false: all of them closed

    public GameWatch()
    {
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, (_, _) => Check(), Dispatcher.CurrentDispatcher);
    }

    public bool Running => _timer.IsEnabled;

    public bool IsOpen => _open;

    public void Start()
    {
        _timer.Start();
        Check();
    }

    public void Stop()
    {
        _timer.Stop();
        _open = false;
    }

    void Check()
    {
        bool open;
        try { open = AnyRunning(); }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { return; }   // try again on the next tick
        if (open == _open) return;
        _open = open;
        Changed?.Invoke(open);
    }

    // one snapshot of the process list for all the names
    static bool AnyRunning()
    {
        bool found = false;
        foreach (Process p in Process.GetProcesses())
        {
            if (!found && Names.Contains(p.ProcessName, StringComparer.OrdinalIgnoreCase)) found = true;
            p.Dispose();
        }
        return found;
    }
}
