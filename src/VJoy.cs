using System.IO;
using System.Runtime.InteropServices;

namespace MeyDrive;

// The parts of vJoyInterface.dll the engine needs. The DLL comes with the vJoy driver.
static class VJoy
{
    const string Dll = "vJoyInterface.dll";

    public const uint X = 0x30, Y = 0x31, Z = 0x32, RX = 0x33, RY = 0x34, RZ = 0x35, SL0 = 0x36, SL1 = 0x37;

    public enum Status { Own, Free, Busy, Missing, Unknown }

    static VJoy()
    {
        // the driver installs the DLL under Program Files, not on the PATH
        NativeLibrary.SetDllImportResolver(typeof(VJoy).Assembly, (name, asm, path) =>
        {
            if (name != Dll) return IntPtr.Zero;
            string installed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "vJoy", "x64", Dll);
            if (File.Exists(installed) && NativeLibrary.TryLoad(installed, out IntPtr handle)) return handle;
            return NativeLibrary.TryLoad(name, asm, path, out IntPtr fallback) ? fallback : IntPtr.Zero;
        });
    }

    [DllImport(Dll)] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool vJoyEnabled();
    [DllImport(Dll)] public static extern Status GetVJDStatus(uint id);
    [DllImport(Dll)] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool AcquireVJD(uint id);
    [DllImport(Dll)] public static extern void RelinquishVJD(uint id);
    [DllImport(Dll)] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool SetAxis(int value, uint id, uint axis);
    [DllImport(Dll)] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool SetBtn([MarshalAs(UnmanagedType.Bool)] bool value, uint id, byte button);

    // true when the DLL can be loaded at all (driver installed)
    public static bool Available()
    {
        try { return vJoyEnabled(); }
        catch (DllNotFoundException) { return false; }
        catch (EntryPointNotFoundException) { return false; }
    }
}
