using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MeyDrive;

// Mouse steering and keyboard pedals into vJoy device 1. Behaves like the classic FreePIE mouse-steering
// script: same 5 ms tick, same axis range (-16383..16383) and the same ramp times.
sealed class Engine
{
    const uint Device = 1;
    const int TickMs = 5;
    const double Max = 16383, Min = -16383;
    const double CenterReduction = 4.8;

    // per-tick ramps, as calculate_rate(max, ms) in the script: max / (ms / 5); 0 ms jumps in one tick
    static double Rate(double ms) => ms > 0 ? Max / (ms / TickMs) : Max;
    static readonly double ThrottleUp = Rate(100), ThrottleDown = -Rate(100);
    static readonly double BrakeUp = Rate(80), BrakeDown = -Rate(80);
    static readonly double ClutchUp = Rate(0), ClutchDown = -Rate(10);

    // FreePIE reads keys by scan code (where the key sits, whatever the layout), so these are scan codes too.
    // vJoy button 1..8 in the script's order: B, LShift, LCtrl, C, Z, Space, E, Q
    static readonly uint[] ButtonScans = { 0x30, 0x2A, 0x1D, 0x2E, 0x2C, 0x39, 0x12, 0x10 };
    static readonly int[] ButtonFallback = { 0x42, 0xA0, 0xA2, 0x43, 0x5A, 0x20, 0x45, 0x51 };
    const uint ScanGas = 0x11 /*W*/, ScanBrake = 0x1F /*S*/, ScanClutch = 0x2E /*C*/;

    Thread? _thread;
    volatile bool _running;
    long _mouseDx;

    double _sensitivity = 20;
    public double Sensitivity { get => Volatile.Read(ref _sensitivity); set => Volatile.Write(ref _sensitivity, value); }

    // last values sent, -1..1 for steering and 0..1 for pedals, read by the UI
    public double Steering, Throttle, Brake, Clutch;
    public bool Running => _running;

    // Returns null when started, else a message for the user.
    public string? Start()
    {
        if (_running) return null;
        if (!VJoy.Available()) return Loc.T("ErrNoVJoy");
        switch (VJoy.GetVJDStatus(Device))
        {
            case VJoy.Status.Busy: return Loc.T("ErrBusy");
            case VJoy.Status.Missing: return Loc.T("ErrMissing");
        }
        if (!VJoy.AcquireVJD(Device)) return Loc.T("ErrAcquire");

        _running = true;
        var ready = new ManualResetEventSlim();
        bool mouseOk = false;
        _thread = new Thread(() => Loop(ready, ok => mouseOk = ok)) { IsBackground = true, Priority = ThreadPriority.AboveNormal, Name = "MeyDrive engine" };
        _thread.Start();
        ready.Wait(1000);
        if (!mouseOk)
        {
            Stop();
            return Loc.T("ErrMouse");
        }
        return null;
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;
        _thread?.Join(1000);
        _thread = null;
        // leave the wheel centred and the pedals up for whatever reads the device next
        SendAll(0, Min, Min, Min);
        for (byte b = 1; b <= ButtonScans.Length; b++) VJoy.SetBtn(false, Device, b);
        VJoy.RelinquishVJD(Device);
        Steering = Throttle = Brake = Clutch = 0;
    }

    void Loop(ManualResetEventSlim ready, Action<bool> reportMouse)
    {
        IntPtr hwnd = Native.CreateMessageWindow(WndProc);
        var rid = new Native.RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x02, dwFlags = Native.RIDEV_INPUTSINK, hwndTarget = hwnd };
        reportMouse(hwnd != IntPtr.Zero && Native.RegisterRawInputDevices(new[] { rid }, 1, (uint)Marshal.SizeOf<Native.RAWINPUTDEVICE>()));
        Native.timeBeginPeriod(1);

        // resolve the scan codes with the layout active now
        int[] buttonKeys = new int[ButtonScans.Length];
        for (int i = 0; i < ButtonScans.Length; i++) buttonKeys[i] = Native.ScanToVk(ButtonScans[i], ButtonFallback[i]);
        int keyGas = Native.ScanToVk(ScanGas, 0x57), keyBrake = Native.ScanToVk(ScanBrake, 0x53), keyClutch = Native.ScanToVk(ScanClutch, 0x43);
        var buttonSent = new bool?[buttonKeys.Length];
        ready.Set();

        double steering = 0, reduction = 1, throttle = Min, brake = Min, clutch = Min;
        var clock = Stopwatch.StartNew();
        double next = 0;
        try
        {
            while (_running)
            {
                // Mouse packets (up to 8000 a second) wake the wait early. Take them in, but run the
                // tick only on the 5 ms schedule: the pedal ramps are counted in ticks.
                double wait = next - clock.Elapsed.TotalMilliseconds;
                if (wait > 0)
                {
                    Native.MsgWaitForMultipleObjectsEx(0, IntPtr.Zero, (uint)Math.Ceiling(wait), Native.QS_ALLINPUT, Native.MWMO_INPUTAVAILABLE);
                    Native.PumpMessages();
                    continue;
                }
                if (wait < -50) next = clock.Elapsed.TotalMilliseconds;   // fell far behind (sleep, debugger): don't race
                next += TickMs;

                Native.PumpMessages();

                // buttons, sent only when they change
                for (int i = 0; i < buttonKeys.Length; i++)
                {
                    bool down = Native.Down(buttonKeys[i]);
                    if (buttonSent[i] == down) continue;
                    VJoy.SetBtn(down, Device, (byte)(i + 1));
                    buttonSent[i] = down;
                }

                // steering: slower near the centre, full speed at the ends
                if (steering > 0) reduction = Math.Pow(CenterReduction, 1 - steering / Max);
                else if (steering < 0) reduction = Math.Pow(CenterReduction, 1 - steering / Min);
                long dx = Interlocked.Exchange(ref _mouseDx, 0);
                steering = Math.Clamp(steering + dx * Sensitivity / reduction, Min, Max);

                clutch = Math.Clamp(clutch + (Native.Down(keyClutch) ? ClutchUp : ClutchDown), Min, Max);
                throttle = Math.Clamp(throttle + (Native.Down(keyGas) ? ThrottleUp : ThrottleDown), Min, Max);
                brake = Math.Clamp(brake + (Native.Down(keyBrake) ? BrakeUp : BrakeDown), Min, Max);

                SendAll(Math.Round(steering), throttle, brake, clutch);
                Steering = steering / Max;
                Throttle = (throttle - Min) / (Max - Min);
                Brake = (brake - Min) / (Max - Min);
                Clutch = (clutch - Min) / (Max - Min);
            }
        }
        finally
        {
            Native.timeEndPeriod(1);
            Native.DestroyWindow(hwnd);
        }
    }

    static void SendAll(double steering, double throttle, double brake, double clutch)
    {
        // -16383..16383 onto vJoy's 1..32767. FreePIE adds 16382 instead, so its resting pedal is -1,
        // which the driver clamps to 0: the same position.
        VJoy.SetAxis((int)steering + 16384, Device, VJoy.X);
        VJoy.SetAxis((int)throttle + 16384, Device, VJoy.Y);
        VJoy.SetAxis((int)clutch + 16384, Device, VJoy.Z);
        VJoy.SetAxis((int)brake + 16384, Device, VJoy.RZ);
    }

    IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == Native.WM_INPUT)
        {
            int dx = Native.ReadRawMouseDx(lParam);
            if (dx != 0) Interlocked.Add(ref _mouseDx, dx);
            return IntPtr.Zero;
        }
        return Native.DefWindowProc(hwnd, msg, wParam, lParam);
    }
}
