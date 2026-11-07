using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace MeyDrive;

public partial class MainWindow : Window
{
    const int HotkeyId = 1, HotkeyTestId = 2;

    readonly Engine _engine = new();
    readonly Settings _settings = Settings.Load();
    readonly bool _startEngine;
    bool _liveOn;
    IntPtr _hwnd;
    bool _capturing, _closing;
    // the hint under the hotkey, kept so it can be shown again in the other language
    string _hintKey = "CursorHint", _hintArg = "";
    bool _hintWarn;
    Forms.NotifyIcon? _tray;
    Forms.ToolStripMenuItem? _miEngine, _miCursor, _miShow, _miClose;

    public MainWindow(bool startEngine)
    {
        _startEngine = startEngine;
        Loc.Apply(_settings.Language);
        InitializeComponent();
        ShowLanguage();
        SensitivitySlider.Value = _settings.Sensitivity;
        _engine.Sensitivity = _settings.Sensitivity;
        SensitivityValue.Text = FormatSensitivity(_settings.Sensitivity);
        HotkeyText.Text = Settings.KeyText(_settings.HotkeyMods, _settings.HotkeyKey);
        ShowHotkeyHint("CursorHint", false);
        CreditText.Text = "v" + typeof(MainWindow).Assembly.GetName().Version?.ToString(3) + "  ·  by weojiro";

        StartupSwitch.IsChecked = Startup.Enabled;
        SourceInitialized += OnSourceInitialized;
        CreateTray();
    }

    void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(_hwnd).AddHook(WndProc);
        int corner = Native.DWMWCP_ROUND;
        Native.DwmSetWindowAttribute(_hwnd, Native.DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
        if (!RegisterHotkey()) ShowHotkeyHint("KeyTaken", true, HotkeyText.Text);
        if (_startEngine) StartEngine();
        UpdateStatus();
    }

    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Native.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            ToggleCursor();
            handled = true;
        }
        return IntPtr.Zero;
    }

    // ---- engine ----

    void OnEngineButton(object sender, RoutedEventArgs e)
    {
        if (_engine.Running) StopEngine();
        else StartEngine();
    }

    void StartEngine()
    {
        string? error = _engine.Start();
        if (error != null)
        {
            UpdateStatus(error);
            return;
        }
        SetLive(true);
        UpdateStatus();
    }

    void StopEngine()
    {
        _engine.Stop();
        SetLive(false);
        UpdateStatus();
    }

    void UpdateStatus(string? error = null)
    {
        bool on = _engine.Running;
        StatusDot.Fill = (Brush)FindResource(on ? "On" : "Dim");
        StatusText.Text = Loc.T(on ? "On" : "Off");
        StatusText.Foreground = (Brush)FindResource(on ? "On" : "Text");
        EngineButtonText.Text = Loc.T(on ? "Stop" : "Start");
        EngineButton.Background = on ? new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x31)) : (Brush)FindResource("AccentGradient");
        EngineCard.BorderBrush = (Brush)FindResource(on ? "ActiveLine" : "CardLine");
        StatusDetail.Text = error ?? (on ? Loc.T("Sending") : "");
        StatusDetail.Visibility = StatusDetail.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        StatusDetail.Foreground = error != null ? (Brush)FindResource("Brake") : (Brush)FindResource("Sub");
        if (_miEngine != null) _miEngine.Text = Loc.T(on ? "TrayStop" : "TrayStart");
    }

    // once per rendered frame, and only while the window is visible
    void SetLive(bool on)
    {
        if (on == _liveOn) return;
        _liveOn = on;
        if (on) CompositionTarget.Rendering += OnRender;
        else
        {
            CompositionTarget.Rendering -= OnRender;
            ShowSteering(0);
        }
    }

    void OnRender(object? sender, EventArgs e)
    {
        if (IsVisible) ShowSteering(_engine.Steering);
    }

    void ShowSteering(double steering)
    {
        double s = Math.Clamp(steering, -1, 1);
        ((ScaleTransform)SteerLeft.RenderTransform).ScaleX = Math.Max(0, -s);
        ((ScaleTransform)SteerRight.RenderTransform).ScaleX = Math.Max(0, s);
    }

    // ---- language ----

    void OnLanguageTr(object sender, RoutedEventArgs e) => SetLanguage("tr");

    void OnLanguageEn(object sender, RoutedEventArgs e) => SetLanguage("en");

    void SetLanguage(string lang)
    {
        if (lang == Loc.Lang) return;
        Loc.Apply(lang);
        _settings.Language = lang;
        _settings.Save();
        ShowLanguage();
        UpdateStatus();
        if (_capturing) HotkeyText.Text = Loc.T("PressKey");
        ShowHotkeyHint(_hintKey, _hintWarn, _hintArg);
        UpdateTrayText();
    }

    // the selected language gets the red frame
    void ShowLanguage()
    {
        bool en = Loc.Lang == "en";
        LangTr.BorderBrush = (Brush)FindResource(en ? "NoBorder" : "Accent");
        LangTr.Opacity = en ? 0.45 : 1;
        LangEn.BorderBrush = (Brush)FindResource(en ? "Accent" : "NoBorder");
        LangEn.Opacity = en ? 1 : 0.45;
    }

    // ---- startup ----

    void OnStartupSwitch(object sender, RoutedEventArgs e)
    {
        Startup.Enabled = StartupSwitch.IsChecked == true;
        StartupSwitch.IsChecked = Startup.Enabled;
    }

    // ---- sensitivity ----

    void OnSensitivityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsInitialized) return;
        _settings.Sensitivity = e.NewValue;
        _engine.Sensitivity = e.NewValue;
        SensitivityValue.Text = FormatSensitivity(e.NewValue);
        _settings.Save();
    }

    void OnSensitivityWheel(object sender, MouseWheelEventArgs e)
    {
        SensitivitySlider.Value = Math.Clamp(SensitivitySlider.Value + (e.Delta > 0 ? 0.5 : -0.5),
                                             SensitivitySlider.Minimum, SensitivitySlider.Maximum);
        e.Handled = true;
    }

    static string FormatSensitivity(double v) => v.ToString(v % 1 == 0 ? "0" : "0.0", CultureInfo.CurrentCulture);

    // ---- cursor lock ----

    void ToggleCursor()
    {
        CursorLock.Toggle();
        if (_miCursor != null) _miCursor.Checked = CursorLock.Locked;
    }

    bool RegisterHotkey() => Native.RegisterHotKey(_hwnd, HotkeyId, _settings.HotkeyMods | 0x4000 /*MOD_NOREPEAT*/, _settings.HotkeyKey);

    void OnHotkeyButton(object sender, RoutedEventArgs e)
    {
        if (_capturing) return;
        _capturing = true;
        // while picking, the old keys should land here and not toggle the cursor
        Native.UnregisterHotKey(_hwnd, HotkeyId);
        HotkeyText.Text = Loc.T("PressKey");
        ShowHotkeyHint("KeyRule", false);
        HotkeyButton.Focus();
    }

    void OnHotkeyKeyDown(object sender, KeyEventArgs e)
    {
        if (!_capturing) return;
        e.Handled = true;
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin) return;
        if (key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None) { EndCapture(); return; }

        uint mods = 0;
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) mods |= Settings.ModControl;
        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) mods |= Settings.ModAlt;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) mods |= Settings.ModShift;
        // a lone letter would fire while typing or driving
        if (mods == 0 && (key < Key.F1 || key > Key.F24))
        {
            ShowHotkeyHint("KeyAlone", true);
            return;
        }
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (!Native.RegisterHotKey(_hwnd, HotkeyTestId, mods | 0x4000, vk))
        {
            ShowHotkeyHint("KeyTaken", true, Settings.KeyText(mods, vk));
            return;
        }
        Native.UnregisterHotKey(_hwnd, HotkeyTestId);
        _settings.HotkeyMods = mods;
        _settings.HotkeyKey = vk;
        _settings.Save();
        EndCapture();
    }

    void OnHotkeyLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (_capturing) EndCapture();
    }

    void EndCapture()
    {
        _capturing = false;
        string text = Settings.KeyText(_settings.HotkeyMods, _settings.HotkeyKey);
        HotkeyText.Text = text;
        if (RegisterHotkey()) ShowHotkeyHint("CursorHint", false);
        else ShowHotkeyHint("KeyTaken", true, text);
        UpdateTrayText();
        Keyboard.ClearFocus();
    }

    void ShowHotkeyHint(string key, bool warn, string arg = "")
    {
        _hintKey = key; _hintWarn = warn; _hintArg = arg;
        HotkeyHint.Text = Loc.T(key, arg);
        HotkeyHint.Foreground = (Brush)FindResource(warn ? "Brake" : "Sub");
    }

    // ---- window and tray ----

    void OnMinimize(object sender, RoutedEventArgs e) => Hide();

    void OnClose(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        _closing = true;
        SetLive(false);
        _engine.Stop();
        CursorLock.Release();
        if (_hwnd != IntPtr.Zero) Native.UnregisterHotKey(_hwnd, HotkeyId);
        if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }
        base.OnClosing(e);
        Application.Current.Shutdown();
    }

    void ShowFromTray()
    {
        if (_closing) return;
        Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
    }

    void UpdateTrayText()
    {
        if (_miEngine == null || _miCursor == null || _miShow == null || _miClose == null) return;
        _miEngine.Text = Loc.T(_engine.Running ? "TrayStop" : "TrayStart");
        _miCursor.Text = Loc.T("TrayCursor", Settings.KeyText(_settings.HotkeyMods, _settings.HotkeyKey));
        _miShow.Text = Loc.T("TrayShow");
        _miClose.Text = Loc.T("Close");
    }

    void CreateTray()
    {
        System.Drawing.Icon icon;
        using (var stream = Application.GetResourceStream(new Uri("pack://application:,,,/app.ico")).Stream)
            icon = new System.Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize);

        var version = new Forms.ToolStripMenuItem("Mey Drive v" + typeof(MainWindow).Assembly.GetName().Version?.ToString(3)) { Tag = TrayMenu.Dimmed };
        _miEngine = new Forms.ToolStripMenuItem(Loc.T("TrayStart"));
        _miEngine.Click += (_, _) => OnEngineButton(this, new RoutedEventArgs());
        _miCursor = new Forms.ToolStripMenuItem(Loc.T("TrayCursor", Settings.KeyText(_settings.HotkeyMods, _settings.HotkeyKey)));
        _miCursor.Click += (_, _) => ToggleCursor();
        _miShow = new Forms.ToolStripMenuItem(Loc.T("TrayShow"));
        _miShow.Click += (_, _) => ShowFromTray();
        _miClose = new Forms.ToolStripMenuItem(Loc.T("Close"));
        _miClose.Click += (_, _) => Close();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.AddRange(new Forms.ToolStripItem[]
        {
            version, new Forms.ToolStripSeparator(),
            _miEngine, _miCursor, new Forms.ToolStripSeparator(),
            _miShow, _miClose
        });
        menu.Opening += (_, _) => _miCursor.Checked = CursorLock.Locked;
        TrayMenu.Style(menu);

        _tray = new Forms.NotifyIcon { Icon = icon, Text = "Mey Drive", ContextMenuStrip = menu, Visible = true };
        _tray.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) ShowFromTray(); };
    }
}
