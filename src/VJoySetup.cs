using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace MeyDrive;

// Installs the bundled vJoy driver and creates device 1, so the user only presses Start. A driver needs
// admin rights: this process starts a second copy of itself as admin, which covers both steps with one prompt.
static class VJoySetup
{
    public const string ElevatedArg = "--setup-vjoy";
    const uint Device = 1;
    const string InstallerResource = "vJoySetup.exe";

    static string ConfigTool => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "vJoy", "x64", "vJoyConfig.exe");

    // an existing device 1 is never changed: another feeder may rely on its setup
    public static bool Needed => !VJoy.Available() || VJoy.GetVJDStatus(Device) == VJoy.Status.Missing;

    // Returns null on success, else a message for the user.
    public static Task<string?> InstallAsync() => Task.Run<string?>(() =>
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, ElevatedArg) { UseShellExecute = true, Verb = "runas" });
            if (process == null) return Loc.T("ErrSetup");
            process.WaitForExit();
            if (process.ExitCode != 0) return Loc.T("ErrSetup");
            return WaitForDevice() ? null : Loc.T("ErrSetupRestart");
        }
        catch (Win32Exception e) when (e.NativeErrorCode == 1223)   // the user said no to the admin prompt
        {
            return Loc.T("ErrSetupCancelled");
        }
        catch (Win32Exception)
        {
            return Loc.T("ErrSetup");
        }
    });

    // Runs in the admin copy. The exit code tells the normal copy how it went.
    public static int RunElevated()
    {
        try
        {
            if (!File.Exists(ConfigTool)) RunInstaller();
            if (!File.Exists(ConfigTool)) return 2;
            // the driver can need a moment before the DLL answers
            if (!WaitForDevice())
            {
                if (!VJoy.Available()) Run(ConfigTool, "enable on", TimeSpan.FromSeconds(30));   // installed but switched off
                if (Needed) Run(ConfigTool, Device + " -a X Y Z Rz -b 8", TimeSpan.FromSeconds(30));
            }
            return WaitForDevice() ? 0 : 3;
        }
        catch (Exception e) when (e is IOException or Win32Exception or UnauthorizedAccessException or InvalidOperationException)
        {
            return 1;
        }
    }

    static bool WaitForDevice()
    {
        for (int i = 0; i < 10; i++)
        {
            if (!Needed) return true;
            Thread.Sleep(500);
        }
        return false;
    }

    static void RunInstaller()
    {
        string dir = Path.Combine(Path.GetTempPath(), "MeyDrive-" + Guid.NewGuid().ToString("N"));
        // admins only: nothing else may swap the file before it runs with admin rights
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(true, false);
        foreach (WellKnownSidType sid in new[] { WellKnownSidType.BuiltinAdministratorsSid, WellKnownSidType.LocalSystemSid })
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(sid, null), FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        new DirectoryInfo(dir).Create(security);
        try
        {
            string file = Path.Combine(dir, InstallerResource);
            using (Stream resource = typeof(VJoySetup).Assembly.GetManifestResourceStream(InstallerResource)!)
            using (FileStream output = File.Create(file))
                resource.CopyTo(output);
            Run(file, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART", TimeSpan.FromMinutes(3));
        }
        finally
        {
            try { Directory.Delete(dir, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    static void Run(string file, string args, TimeSpan timeout)
    {
        using Process process = Process.Start(new ProcessStartInfo(file, args) { UseShellExecute = false, CreateNoWindow = true })!;
        process.WaitForExit((int)timeout.TotalMilliseconds);
    }
}
