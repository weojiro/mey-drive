using System.Windows;

namespace MeyDrive;

// Turkish and English text. XAML reads it as {DynamicResource L.Key}, code as Loc.T("Key"),
// so switching the language updates every label at once.
static class Loc
{
    static readonly Dictionary<string, (string Tr, string En)> Text = new()
    {
        ["Minimize"] = ("Tepsiye küçült", "Minimize to tray"),
        ["Close"] = ("Kapat", "Close"),
        ["Off"] = ("KAPALI", "OFF"),
        ["On"] = ("AKTİF", "ON"),
        ["Start"] = ("BAŞLAT", "START"),
        ["Stop"] = ("DURDUR", "STOP"),
        ["Sending"] = ("vJoy'a gönderiliyor · 5 ms", "Sending to vJoy · 5 ms"),
        ["Steering"] = ("DİREKSİYON", "STEERING"),
        ["Sensitivity"] = ("Hassasiyet", "Sensitivity"),
        ["SensitivityHint"] = ("Düşük değer = direksiyon daha yavaş döner", "Lower = the wheel turns slower"),
        ["Live"] = ("Canlı direksiyon", "Live steering"),
        ["LiveHint"] = ("Script açıkken fareyi oynat", "Move the mouse while the script runs"),
        ["Left"] = ("SOL", "LEFT"),
        ["Right"] = ("SAĞ", "RIGHT"),
        ["Cursor"] = ("İMLEÇ", "CURSOR"),
        ["HideCursor"] = ("İmleç gizleme", "Hide cursor"),
        ["CursorHint"] = ("Sol alta kilitler · değiştirmek için tıkla", "Locks it bottom-left · click to change"),
        ["PressKey"] = ("Tuşa bas…", "Press a key…"),
        ["KeyRule"] = ("Ctrl, Alt, Shift ya da bir F tuşu gerekir · Esc: vazgeç", "Needs Ctrl, Alt, Shift or an F key · Esc: cancel"),
        ["KeyAlone"] = ("Bu tuş tek başına olmaz", "This key can't be used alone"),
        ["KeyTaken"] = ("{0} başka bir programda kullanılıyor", "{0} is used by another program"),
        ["System"] = ("SİSTEM", "SYSTEM"),
        ["StartWithWindows"] = ("Assetto Corsa ile başlat", "Start with Assetto Corsa"),
        ["StartWithWindowsHint"] = ("CM veya oyun açılınca script kendiliğinden başlar", "The script starts when CM or the game opens"),
        ["TrayStart"] = ("Scripti başlat", "Start script"),
        ["TrayStop"] = ("Scripti durdur", "Stop script"),
        ["TrayCursor"] = ("İmleci gizle ({0})", "Hide cursor ({0})"),
        ["TrayShow"] = ("Pencereyi göster", "Show window"),
        ["ErrNoVJoy"] = ("vJoy kurulu değil. BAŞLAT'a basınca kurulur.", "vJoy is not installed. Press START to install it."),
        ["SettingUp"] = ("vJoy kuruluyor…", "Installing vJoy…"),
        ["ErrSetup"] = ("vJoy kurulamadı. Tekrar dene.", "Could not install vJoy. Try again."),
        ["ErrSetupRestart"] = ("vJoy kuruldu. Uygulamayı kapatıp yeniden aç.", "vJoy is installed. Close and reopen the app."),
        ["ErrSetupCancelled"] = ("vJoy kurulumu için Windows iznine Evet demelisin.", "Say Yes to the Windows prompt to install vJoy."),
        ["ErrBusy"] = ("vJoy başka bir program tarafından kullanılıyor. FreePIE açıksa kapat.", "vJoy is used by another program. Close FreePIE if it is open."),
        ["ErrMissing"] = ("vJoy cihazı ayarlı değil. BAŞLAT'a basınca ayarlanır.", "The vJoy device is not set up. Press START to set it up."),
        ["ErrAcquire"] = ("vJoy cihazına bağlanılamadı.", "Could not connect to the vJoy device."),
        ["ErrMouse"] = ("Fare okunamadı (Raw Input kaydı başarısız).", "Could not read the mouse (Raw Input registration failed)."),
    };

    public static string Lang { get; private set; } = "tr";

    public static string T(string key) => Lang == "en" ? Text[key].En : Text[key].Tr;

    public static string T(string key, string arg) => string.Format(T(key), arg);

    public static void Apply(string lang)
    {
        Lang = lang == "en" ? "en" : "tr";
        var resources = Application.Current.Resources;
        foreach (string key in Text.Keys) resources["L." + key] = T(key);
    }
}
