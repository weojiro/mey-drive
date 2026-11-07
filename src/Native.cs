using System.Runtime.InteropServices;

namespace MeyDrive;

static class Native
{
    public const uint WM_INPUT = 0x00FF, WM_HOTKEY = 0x0312;
    public const uint RIDEV_INPUTSINK = 0x00000100;
    public const uint QS_ALLINPUT = 0x04FF, MWMO_INPUTAVAILABLE = 0x0004;
    const uint RID_INPUT = 0x10000003;
    const int RIM_TYPEMOUSE = 0;
    const ushort MOUSE_MOVE_ABSOLUTE = 0x01;
    static readonly IntPtr HWND_MESSAGE = new(-3);

    public delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTDEVICE { public ushort usUsagePage, usUsage; public uint dwFlags; public IntPtr hwndTarget; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WNDCLASSEX
    {
        public uint cbSize, style; public IntPtr lpfnWndProc; public int cbClsExtra, cbWndExtra;
        public IntPtr hInstance, hIcon, hCursor, hbrBackground; public string? lpszMenuName, lpszClassName; public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint count, uint size);
    [DllImport("user32.dll")] static extern uint GetRawInputData(IntPtr hRawInput, uint command, IntPtr data, ref uint size, uint headerSize);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern ushort RegisterClassEx(ref WNDCLASSEX wc);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateWindowEx(uint exStyle, string className, string name, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
    [DllImport("user32.dll")] public static extern bool DestroyWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern IntPtr DefWindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool PeekMessage(out MSG msg, IntPtr hwnd, uint min, uint max, uint remove);
    [DllImport("user32.dll")] static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32.dll")] static extern IntPtr DispatchMessage(ref MSG msg);
    [DllImport("user32.dll")] public static extern uint MsgWaitForMultipleObjectsEx(uint count, IntPtr handles, uint ms, uint wakeMask, uint flags);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vk);
    [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(string? name);
    [DllImport("winmm.dll")] public static extern uint timeBeginPeriod(uint ms);
    [DllImport("winmm.dll")] public static extern uint timeEndPeriod(uint ms);

    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint mods, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool ClipCursor(ref RECT r);
    [DllImport("user32.dll")] public static extern bool ClipCursor(IntPtr none);
    [DllImport("user32.dll")] public static extern IntPtr CreateCursor(IntPtr inst, int x, int y, int w, int h, byte[] and, byte[] xor);
    [DllImport("user32.dll")] public static extern IntPtr CopyIcon(IntPtr h);
    [DllImport("user32.dll")] public static extern bool SetSystemCursor(IntPtr h, uint id);
    [DllImport("user32.dll")] public static extern bool SystemParametersInfo(uint action, uint param, IntPtr pv, uint flags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int w, int h, uint flags);
    [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWA_BORDER_COLOR = 34, DWMWCP_ROUND = 2;
    public const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);
    public const uint SWP_NOSIZE = 0x1, SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10;

    // The window class is registered once and keeps a raw pointer to this delegate for the life of the
    // process, so it must never be collected. Each start only swaps the handler it forwards to.
    static readonly WndProcDelegate ClassProc = (hwnd, msg, wParam, lParam) =>
        _handler != null ? _handler(hwnd, msg, wParam, lParam) : DefWindowProc(hwnd, msg, wParam, lParam);
    static volatile WndProcDelegate? _handler;
    static bool _classRegistered;

    public static IntPtr CreateMessageWindow(WndProcDelegate proc)
    {
        const string className = "MeyDriveRawInput";
        _handler = proc;
        IntPtr inst = GetModuleHandle(null);
        if (!_classRegistered)
        {
            var wc = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(ClassProc),
                hInstance = inst,
                lpszClassName = className,
            };
            _classRegistered = RegisterClassEx(ref wc) != 0;
        }
        return CreateWindowEx(0, className, "", 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, inst, IntPtr.Zero);
    }

    public static void PumpMessages()
    {
        while (PeekMessage(out MSG msg, IntPtr.Zero, 0, 0, 1 /*PM_REMOVE*/))
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    public static bool Down(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    [DllImport("user32.dll")] static extern uint MapVirtualKey(uint code, uint mapType);

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_POWER_THROTTLING_STATE { public uint Version, ControlMask, StateMask; }
    [DllImport("kernel32.dll")] static extern bool SetProcessInformation(IntPtr process, int infoClass, ref PROCESS_POWER_THROTTLING_STATE info, int size);
    [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();

    // Windows 11 drops timeBeginPeriod(1) back to 15.6 ms once none of a process's windows are visible
    // (game fullscreen on top, or sitting in the tray). Measured in a drive log: a third of the ticks
    // then came 15.6 ms late in bursts. Opt out of that, and of EcoQoS slowing the engine thread.
    public static void KeepTimerResolution()
    {
        var state = new PROCESS_POWER_THROTTLING_STATE
        {
            Version = 1,
            ControlMask = 0x1 /*EXECUTION_SPEED*/ | 0x4 /*IGNORE_TIMER_RESOLUTION*/,
            StateMask = 0,   // 0 = never throttle, always honor the timer request
        };
        SetProcessInformation(GetCurrentProcess(), 4 /*ProcessPowerThrottling*/, ref state, Marshal.SizeOf<PROCESS_POWER_THROTTLING_STATE>());
    }

    // the key at this scan code position in the current layout (MAPVK_VSC_TO_VK_EX keeps left/right Shift and Ctrl apart)
    public static int ScanToVk(uint scan, int fallback)
    {
        uint vk = MapVirtualKey(scan, 3);
        return vk != 0 ? (int)vk : fallback;
    }

    // relative X movement in a WM_INPUT mouse packet; 0 for anything else
    public static unsafe int ReadRawMouseDx(IntPtr hRawInput)
    {
        int headerSize = IntPtr.Size == 8 ? 24 : 16;
        byte* buffer = stackalloc byte[64];
        uint size = 64;
        if (GetRawInputData(hRawInput, RID_INPUT, (IntPtr)buffer, ref size, (uint)headerSize) == unchecked((uint)-1)) return 0;
        if (*(int*)buffer != RIM_TYPEMOUSE) return 0;
        ushort flags = *(ushort*)(buffer + headerSize);
        if ((flags & MOUSE_MOVE_ABSOLUTE) != 0) return 0;   // tablets and remote desktop send positions, not deltas
        return *(int*)(buffer + headerSize + 12);           // RAWMOUSE.lLastX
    }
}
