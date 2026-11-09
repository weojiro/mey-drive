using Microsoft.Win32;

namespace MeyDrive;

// "Start with Assetto Corsa": Mey Drive starts with Windows, waits in the tray with the script off,
// and GameWatch turns the script on while Content Manager or the game is open.
static class Startup
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run", Name = "MeyDrive";

    static string Command => "\"" + Environment.ProcessPath + "\" --tray";

    public static bool Enabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(Name) is string;
        }
        set
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RunKey);
                if (value) key.SetValue(Name, Command);
                else key.DeleteValue(Name, false);
            }
            catch (UnauthorizedAccessException) { }
            catch (System.Security.SecurityException) { }
        }
    }

    // keep the entry pointing at this exe with today's arguments (the exe may have moved, or 1.0 wrote --start)
    public static void Refresh()
    {
        if (Enabled) Enabled = true;
    }
}
