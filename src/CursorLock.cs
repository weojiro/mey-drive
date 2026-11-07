using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace MeyDrive;

// Hides the cursor and pins it to the bottom-left pixel of the screen.
// The game still gets raw mouse movement; the right-edge app bar in AC stays closed.
static class CursorLock
{
    const uint SPI_SETCURSORS = 0x57;
    static readonly uint[] CursorIds = { 32512, 32513, 32514, 32515, 32516, 32642, 32643, 32644, 32645, 32646, 32648, 32649, 32650 };

    static IntPtr _blank;
    static Native.RECT _corner;
    static DispatcherTimer? _timer;

    public static bool Locked { get; private set; }

    public static void Toggle()
    {
        if (Locked) Release();
        else Lock();
    }

    public static void Lock()
    {
        if (Locked) return;
        if (_blank == IntPtr.Zero)
        {
            var and = new byte[32 * 32 / 8];
            Array.Fill(and, (byte)0xFF);
            _blank = Native.CreateCursor(IntPtr.Zero, 0, 0, 32, 32, and, new byte[32 * 32 / 8]);
        }
        var screen = Forms.Screen.FromHandle(Native.GetForegroundWindow()).Bounds;
        _corner = new Native.RECT { Left = screen.Left, Top = screen.Bottom - 1, Right = screen.Left + 1, Bottom = screen.Bottom };
        // SetSystemCursor takes ownership of the handle, so each cursor gets its own copy
        foreach (uint id in CursorIds) Native.SetSystemCursor(Native.CopyIcon(_blank), id);
        Native.ClipCursor(ref _corner);
        Locked = true;

        // Windows drops the clip on Alt+Tab, UAC and the like: put it back
        _timer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, (_, _) => { if (Locked) Native.ClipCursor(ref _corner); }, Dispatcher.CurrentDispatcher);
        _timer.Start();
    }

    public static void Release()
    {
        Native.ClipCursor(IntPtr.Zero);
        _timer?.Stop();
        if (!Locked) return;
        Native.SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, 0);
        Locked = false;
    }
}
