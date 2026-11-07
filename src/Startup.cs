using Microsoft.Win32;

namespace MeyDrive;

// "start with Windows" as a value under the current user's Run key
static class Startup
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run", Name = "MeyDrive";

    static string Command => "\"" + Environment.ProcessPath + "\" --start --tray";

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
}
