using System.Globalization;
using System.IO;
using System.Windows.Input;

namespace MeyDrive;

// %LOCALAPPDATA%\MeyDrive\settings.ini, so only the exe has to sit wherever the user keeps it.
sealed class Settings
{
    public const uint ModAlt = 0x1, ModControl = 0x2, ModShift = 0x4;

    public double Sensitivity = 20;
    public uint HotkeyMods = ModControl;
    public uint HotkeyKey = 0x30;   // 0
    // first run follows the Windows language: Turkish gets "tr", everything else "en"
    public string Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr" ? "tr" : "en";

    static readonly string Dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeyDrive");
    static readonly string Path = System.IO.Path.Combine(Dir, "settings.ini");
    // where 0.1.0 kept it
    static readonly string OldPath = System.IO.Path.Combine(AppContext.BaseDirectory, "settings.ini");

    public static Settings Load()
    {
        var s = new Settings();
        try
        {
            if (!File.Exists(Path) && File.Exists(OldPath))
            {
                Directory.CreateDirectory(Dir);
                File.Move(OldPath, Path);
            }
            foreach (string line in File.ReadAllLines(Path))
            {
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                string key = line[..eq].Trim(), value = line[(eq + 1)..].Trim();
                if (key == "sensitivity" && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) s.Sensitivity = Math.Clamp(d, 1, 60);
                if (key == "hotkey_mods" && uint.TryParse(value, out uint m)) s.HotkeyMods = m;
                if (key == "hotkey_key" && uint.TryParse(value, out uint k) && k != 0) s.HotkeyKey = k;
                if (key == "language" && value is "tr" or "en") s.Language = value;
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return s;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllLines(Path, new[]
            {
                "sensitivity=" + Sensitivity.ToString(CultureInfo.InvariantCulture),
                "hotkey_mods=" + HotkeyMods,
                "hotkey_key=" + HotkeyKey,
                "language=" + Language,
            });
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public static string KeyText(uint mods, uint vk)
    {
        string text = "";
        if ((mods & ModControl) != 0) text += "Ctrl + ";
        if ((mods & ModAlt) != 0) text += "Alt + ";
        if ((mods & ModShift) != 0) text += "Shift + ";
        Key key = KeyInterop.KeyFromVirtualKey((int)vk);
        if (key >= Key.D0 && key <= Key.D9) return text + (char)('0' + (key - Key.D0));
        if (key >= Key.NumPad0 && key <= Key.NumPad9) return text + "Num " + (key - Key.NumPad0);
        return text + key;
    }
}
